using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.ViewModels;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class RouteConfirmationPage : ContentPage
{
    private const string CurrentStudentNameKey = "CurrentStudentName";
    private const string CurrentInstructorNameKey = "CurrentInstructorName";

    private readonly LessonReviewViewModel _viewModel = new();

    private bool _mapInitialized;
    private string? _sessionId;
    private string? _loadedSessionId;
    private RouteSession? _session;

    public string? SessionId
    {
        get => _sessionId;
        set => _sessionId = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public RouteConfirmationPage()
    {
        InitializeComponent();

        BindingContext = _viewModel;

        InstructorSignaturePad.Lines = new ObservableCollection<IDrawingLine>();
        StudentSignaturePad.Lines = new ObservableCollection<IDrawingLine>();

        InstructorSignaturePad.DrawingLineCompleted += SignaturePad_DrawingLineCompleted;
        StudentSignaturePad.DrawingLineCompleted += SignaturePad_DrawingLineCompleted;

        EnsureMapInitialized();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrWhiteSpace(SessionId) || string.Equals(_loadedSessionId, SessionId, StringComparison.Ordinal))
            return;

        await LoadSessionAsync(SessionId);
    }

    private async Task LoadSessionAsync(string sessionId)
    {
        _session = await RouteStorage.LoadAsync(sessionId);

        if (_session is null)
        {
            await DisplayAlertAsync(AppText.RouteReviewNotFoundTitle, AppText.RouteReviewNotFoundMessage, AppText.CommonOk);
            await Shell.Current.GoToAsync("..");
            return;
        }

        _loadedSessionId = sessionId;

        _viewModel.ApplySession(
            _session,
            Preferences.Get(CurrentStudentNameKey, string.Empty),
            Preferences.Get(CurrentInstructorNameKey, string.Empty));

        await _viewModel.RefreshStudentProgressAsync();

        RestoreSignature(InstructorSignaturePad, _session.InstructorSignature);
        RestoreSignature(StudentSignaturePad, _session.StudentSignature);
        UpdateSignatureStatus();
        DrawRoutePreview(_session.Points);
    }

    private void EnsureMapInitialized()
    {
        if (_mapInitialized)
            return;

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer("DrivingSchoolApp"));

        RouteMapView.Map = map;
        RouteMapView.MyLocationEnabled = false;
        RouteMapView.MyLocationFollow = false;

        _mapInitialized = true;
    }

    private void DrawRoutePreview(IReadOnlyList<TrackPoint> points)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        if (points.Count == 0)
            return;

        var ordered = points
            .OrderBy(x => x.Timestamp)
            .Select(x => new Position(x.Latitude, x.Longitude))
            .ToList();

        if (ordered.Count > 1)
        {
            var line = new Polyline
            {
                StrokeWidth = 7,
                StrokeColor = Color.FromArgb("#2F6FD6")
            };

            foreach (var position in ordered)
                line.Positions.Add(position);

            RouteMapView.Drawables.Add(line);
        }

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailStartPin,
            Position = ordered.First()
        });

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailEndPin,
            Position = ordered.Last()
        });

        ZoomToPositions(ordered);
    }

    private void ZoomToPositions(IReadOnlyList<Position> positions)
    {
        if (RouteMapView.Map is null || positions.Count == 0)
            return;

        var world = positions
            .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
            .ToList();

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;

        foreach (var point in world)
        {
            if (point.Item1 < minX) minX = point.Item1;
            if (point.Item2 < minY) minY = point.Item2;
            if (point.Item1 > maxX) maxX = point.Item1;
            if (point.Item2 > maxY) maxY = point.Item2;
        }

        if (Math.Abs(maxX - minX) < 1 && Math.Abs(maxY - minY) < 1)
        {
            var padding = 100d;
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;
        }

        RouteMapView.Map.Navigator.ZoomToBox(new MRect(minX, minY, maxX, maxY));
    }

    private async void StudentNameEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        await _viewModel.RefreshStudentProgressAsync();
    }

    private void LessonItemRow_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not LessonItemSelectionViewModel item)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
    }

    private void ClearInstructorSignatureButton_Clicked(object? sender, EventArgs e)
    {
        InstructorSignaturePad.Clear();
        UpdateSignatureStatus();
    }

    private void ClearStudentSignatureButton_Clicked(object? sender, EventArgs e)
    {
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();
    }

    private void SignaturePad_DrawingLineCompleted(object? sender, DrawingLineCompletedEventArgs e)
    {
        UpdateSignatureStatus();
    }

    private void UpdateSignatureStatus()
    {
        InstructorSignatureStatusLabel.Text = HasSignature(InstructorSignaturePad)
            ? AppText.RouteReviewInstructorSignatureCaptured
            : AppText.RouteReviewInstructorSignaturePrompt;

        StudentSignatureStatusLabel.Text = HasSignature(StudentSignaturePad)
            ? AppText.RouteReviewStudentSignatureCaptured
            : AppText.RouteReviewStudentSignaturePrompt;
    }

    private async void FinalizeLessonButton_Clicked(object? sender, EventArgs e)
    {
        if (_session is null)
            return;

        var studentName = _viewModel.StudentName.Trim();
        var instructorName = _viewModel.InstructorName.Trim();

        if (string.IsNullOrWhiteSpace(studentName))
        {
            await DisplayAlertAsync(AppText.RouteReviewMissingStudentTitle, AppText.RouteReviewMissingStudentMessage, AppText.CommonOk);
            return;
        }

        if (string.IsNullOrWhiteSpace(instructorName))
        {
            await DisplayAlertAsync(AppText.RouteReviewMissingInstructorTitle, AppText.RouteReviewMissingInstructorMessage, AppText.CommonOk);
            return;
        }

        if (!HasSignature(InstructorSignaturePad))
        {
            await DisplayAlertAsync(AppText.RouteReviewInstructorSignatureRequiredTitle, AppText.RouteReviewInstructorSignatureRequiredMessage, AppText.CommonOk);
            return;
        }

        if (!HasSignature(StudentSignaturePad))
        {
            await DisplayAlertAsync(AppText.RouteReviewStudentSignatureRequiredTitle, AppText.RouteReviewStudentSignatureRequiredMessage, AppText.CommonOk);
            return;
        }

        var finalizedAt = DateTimeOffset.UtcNow;
        var selectedItems = _viewModel.GetSelectedItemTypes();

        var student = await StudentProgressStorage.SaveLessonCompletionAsync(
            studentName,
            _session.StudentId,
            _session.Id,
            selectedItems,
            finalizedAt);

        _session.StudentId = student.Id;
        _session.StudentName = student.FullName;
        _session.InstructorName = instructorName;
        _session.TotalDistanceMeters ??= RouteSessionMetrics.CalculateDistanceMeters(_session.Points);
        _session.CompletedItems = selectedItems
            .Select(x => new CompletedLessonItem
            {
                ItemType = x,
                CompletedAt = finalizedAt,
                LessonSessionId = _session.Id
            })
            .ToList();
        _session.InstructorSignature = BuildSignature(InstructorSignaturePad, instructorName, finalizedAt);
        _session.StudentSignature = BuildSignature(StudentSignaturePad, student.FullName, finalizedAt);
        _session.IsFinalized = true;
        _session.FinalizedAt = finalizedAt;

        await RouteStorage.SaveAsync(_session);
        await RouteSnapBackgroundProcessor.EnqueueAsync(_session.Id);

        Preferences.Set(CurrentStudentNameKey, student.FullName);
        Preferences.Set(CurrentInstructorNameKey, instructorName);

        await DisplayAlertAsync(AppText.RouteReviewFinalizedTitle, AppText.RouteReviewFinalizedMessage, AppText.CommonOk);
        await Shell.Current.GoToAsync("//saved-routes");
    }

    private static bool HasSignature(DrawingView signaturePad)
        => signaturePad.Lines?.Any(x => x.Points?.Count > 1) == true;

    private static LessonSignature BuildSignature(
        DrawingView signaturePad,
        string signedByName,
        DateTimeOffset signedAt)
    {
        var strokes = signaturePad.Lines?
            .Where(x => x.Points is { Count: > 0 })
            .Select(x => new SignatureStrokeData
            {
                LineWidth = (float)x.LineWidth,
                Points = x.Points
                    .Select(p => new SignaturePointData
                    {
                        X = p.X,
                        Y = p.Y
                    })
                    .ToList()
            })
            .ToList() ?? new List<SignatureStrokeData>();

        return new LessonSignature
        {
            SignedByName = signedByName,
            SignedAt = signedAt,
            Strokes = strokes
        };
    }

    private static void RestoreSignature(DrawingView signaturePad, LessonSignature? signature)
    {
        signaturePad.Clear();

        if (signature?.Strokes is not { Count: > 0 })
            return;

        var lines = new ObservableCollection<IDrawingLine>();

        foreach (var stroke in signature.Strokes)
        {
            if (stroke.Points.Count == 0)
                continue;

            var line = new DrawingLine
            {
                LineColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black,
                LineWidth = stroke.LineWidth <= 0 ? 4f : stroke.LineWidth
            };

            foreach (var point in stroke.Points)
                line.Points.Add(new PointF(point.X, point.Y));

            lines.Add(line);
        }

        signaturePad.Lines = lines;
    }
}
