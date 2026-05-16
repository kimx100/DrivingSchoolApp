using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class RouteDetailPage : ContentPage
{
    private readonly OsrmRoadSnapService _snapService = new();

    private bool _mapInitialized;
    private bool _loaded;
    private bool _isSnapping;

    private string? _sessionId;
    private RouteSession? _session;
    private SnappedRouteCache? _snapCache;
    private RouteSnapState? _snapState;

    public string? SessionId
    {
        get => _sessionId;
        set
        {
            _sessionId = Uri.UnescapeDataString(value ?? string.Empty);
            _loaded = false;
        }
    }

    public RouteDetailPage()
    {
        InitializeComponent();
        EnsureMapInitialized();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded || string.IsNullOrWhiteSpace(SessionId))
            return;

        await LoadRouteAsync();
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

    private async Task LoadRouteAsync()
    {
        LoadingMessageLabel.Text = AppText.RouteDetailLoadingMessage;
        LoadingOverlay.IsVisible = true;

        _session = await RouteStorage.LoadAsync(SessionId!);

        if (_session is null || _session.Points is not { Count: > 0 })
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlertAsync(AppText.RouteDetailNotFoundTitle, AppText.RouteDetailNotFoundMessage, AppText.CommonOk);
            await Shell.Current.GoToAsync("..");
            return;
        }

        _snapCache = await RouteSnapStorage.LoadAsync(_session.Id);
        _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);

        Title = string.IsNullOrWhiteSpace(_session.StudentName)
            ? _session.StartedAt.LocalDateTime.ToString("dd MMM yyyy")
            : _session.StudentName;

        DrawBestAvailableRoute();
        UpdateRouteInfoOverlay();
        UpdateSnapToolbarText();

        if (_snapState?.Status == RouteSnapWorkStatus.ReadyToView)
        {
            await RouteSnapBackgroundProcessor.MarkViewedAsync(_session.Id);
            _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);
            UpdateSnapToolbarText();
        }

        _loaded = true;
        LoadingOverlay.IsVisible = false;
    }

    private void DrawBestAvailableRoute()
    {
        if (_session is null)
            return;

        if (HasUsableSnappedGeometry())
        {
            DrawCoordinates(_snapCache!.Geometry);
            return;
        }

        DrawTrackPoints(_session.Points);
    }

    private bool HasUsableSnappedGeometry()
    {
        if (_session is null || _snapCache is null)
            return false;

        if (_snapCache.SessionId != _session.Id)
            return false;

        if (_snapCache.SourcePointCount != _session.Points.Count)
            return false;

        return _snapCache.Geometry is { Count: > 1 };
    }

    private void UpdateSnapToolbarText()
    {
        if (_snapState?.Status is RouteSnapWorkStatus.Pending or RouteSnapWorkStatus.Processing)
        {
            SnapToolbarItem.Text = AppText.RouteDetailSnappingInProgressButton;
            SnapToolbarItem.IsEnabled = false;
            return;
        }

        if (_snapState?.Status == RouteSnapWorkStatus.Failed)
        {
            SnapToolbarItem.Text = AppText.RouteDetailRetrySnapButton;
        }
        else if (HasUsableSnappedGeometry())
        {
            SnapToolbarItem.Text = AppText.RouteDetailResnapButton;
        }
        else
        {
            SnapToolbarItem.Text = AppText.RouteDetailSnapButton;
        }

        SnapToolbarItem.IsEnabled = !_isSnapping;
    }

    private void DrawTrackPoints(IReadOnlyList<TrackPoint> points)
    {
        var ordered = points
            .OrderBy(p => p.Timestamp)
            .Select(p => new Position(p.Latitude, p.Longitude))
            .ToList();

        DrawPositions(ordered);
    }

    private void DrawCoordinates(IReadOnlyList<RouteCoordinate> geometry)
    {
        var positions = geometry
            .Select(p => new Position(p.Latitude, p.Longitude))
            .ToList();

        DrawPositions(positions);
    }

    private void DrawPositions(IReadOnlyList<Position> positions)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        if (positions.Count == 0)
            return;

        if (positions.Count > 1)
        {
            var line = new Polyline
            {
                StrokeWidth = 7,
                StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#2F6FD6")
            };

            foreach (var position in positions)
                line.Positions.Add(position);

            RouteMapView.Drawables.Add(line);
        }

        var first = positions.First();
        var last = positions.Last();

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailStartPin,
            Position = first,
            Scale = RouteMapViewportHelper.RouteEndpointPinScale
        });

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailEndPin,
            Position = last,
            Scale = RouteMapViewportHelper.RouteEndpointPinScale
        });

        ZoomToPositions(positions);
    }

    private void ZoomToPositions(IReadOnlyList<Position> positions)
    {
        RouteMapViewportHelper.FitRoute(
            RouteMapView,
            positions,
            accountForBottomOverlay: true,
            debugSource: HasUsableSnappedGeometry() ? "RouteDetailPage Snapped" : "RouteDetailPage Raw");
    }

    private void UpdateRouteInfoOverlay()
    {
        if (_session is null)
        {
            RouteInfoOverlay.IsVisible = false;
            return;
        }

        var duration = _session.EndedAt - _session.StartedAt;
        var distance = _session.TotalDistanceMeters ?? RouteSessionMetrics.CalculateDistanceMeters(_session.Points);
        var hasInstructorSignature = HasSavedSignature(_session.InstructorSignature);
        var hasStudentSignature = HasSavedSignature(_session.StudentSignature);

        RouteDateLabel.Text = _session.StartedAt.LocalDateTime.ToString("dd MMM yyyy");
        RouteDurationLabel.Text = RouteSessionMetrics.FormatDuration(duration);
        RouteDistanceLabel.Text = RouteSessionMetrics.FormatDistance(distance);
        RouteTimeLabel.Text =
            $"{_session.StartedAt.LocalDateTime:HH:mm} - {_session.EndedAt.LocalDateTime:HH:mm}";
        RouteStudentLabel.Text = string.IsNullOrWhiteSpace(_session.StudentName)
            ? "Not assigned"
            : _session.StudentName;
        RouteSourceLabel.Text = HasUsableSnappedGeometry()
            ? "Snapped"
            : "Raw GPS";
        RouteObjectivesLabel.Text = FormatCompletedItems(_session.CompletedItems);

        RouteSignatureStatusLabel.Text = FormatSignatureStatus(hasInstructorSignature, hasStudentSignature);

        RestoreSignature(InstructorSignaturePreview, _session.InstructorSignature);
        RestoreSignature(StudentSignaturePreview, _session.StudentSignature);

        InstructorSignaturePreviewContainer.IsVisible = hasInstructorSignature;
        StudentSignaturePreviewContainer.IsVisible = hasStudentSignature;
        SignaturePreviewGrid.IsVisible = hasInstructorSignature || hasStudentSignature;
        RouteInfoOverlay.IsVisible = true;
    }

    private static string FormatCompletedItems(IReadOnlyList<CompletedLessonItem>? completedItems)
    {
        if (completedItems is not { Count: > 0 })
            return "No objectives saved.";

        var catalog = LessonCatalogService.GetAll().ToDictionary(x => x.ItemType);

        return string.Join(", ",
            completedItems
                .OrderBy(x => x.CompletedAt)
                .Select(x =>
                {
                    if (!catalog.TryGetValue(x.ItemType, out var definition))
                        return x.ItemType.ToString();

                    return definition.DisplayNameDa ?? definition.DisplayName;
                }));
    }

    private static string FormatSignatureStatus(bool hasInstructorSignature, bool hasStudentSignature)
    {
        return (hasInstructorSignature, hasStudentSignature) switch
        {
            (true, true) => "Signatures saved: instructor and student",
            (true, false) => "Signature saved: instructor only",
            (false, true) => "Signature saved: student only",
            _ => "No signatures saved"
        };
    }

    private static bool HasSavedSignature(LessonSignature? signature)
        => signature?.Strokes?.Any(x => x.Points.Count > 1) == true;

    private static void RestoreSignature(DrawingView signaturePreview, LessonSignature? signature)
    {
        signaturePreview.Lines = new ObservableCollection<IDrawingLine>();

        if (!HasSavedSignature(signature))
            return;

        var lines = new ObservableCollection<IDrawingLine>();

        foreach (var stroke in signature!.Strokes)
        {
            if (stroke.Points.Count == 0)
                continue;

            var line = new DrawingLine
            {
                LineColor = Colors.Black,
                LineWidth = stroke.LineWidth <= 0 ? 3f : stroke.LineWidth
            };

            foreach (var point in stroke.Points)
                line.Points.Add(new PointF(point.X, point.Y));

            lines.Add(line);
        }

        signaturePreview.Lines = lines;
    }

    private async void SnapToolbarItem_Clicked(object? sender, EventArgs e)
    {
        if (_session is null || _isSnapping)
            return;

        var hadUsableSnappedGeometry = HasUsableSnappedGeometry();
        var existingSnapCache = _snapCache;

        _isSnapping = true;
        UpdateSnapToolbarText();

        LoadingMessageLabel.Text = hadUsableSnappedGeometry
            ? AppText.RouteDetailResnappingMessage
            : AppText.RouteDetailSnappingMessage;
        LoadingOverlay.IsVisible = true;

        try
        {
            var result = await _snapService.TrySnapAsync(_session);

            if (!result.Success || result.Cache is null)
            {
                if (hadUsableSnappedGeometry)
                {
                    _snapCache = existingSnapCache;
                    DrawBestAvailableRoute();
                    UpdateRouteInfoOverlay();
                }

                await DisplayAlertAsync(
                    AppText.RouteDetailSnapUnavailableTitle,
                    BuildManualSnapFailureMessage(result.Message, hadUsableSnappedGeometry),
                    AppText.CommonOk);
                return;
            }

            _snapCache = result.Cache;
            await RouteSnapStorage.SaveAsync(result.Cache);
            await RouteSnapBackgroundProcessor.MarkManualSnapSucceededAsync(_session.Id);

            _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);

            DrawBestAvailableRoute();
            UpdateRouteInfoOverlay();
            UpdateSnapToolbarText();

            await DisplayAlertAsync(
                AppText.RouteDetailSnappedTitle,
                AppText.RouteDetailSnappedMessage,
                AppText.CommonOk);
        }
        finally
        {
            _isSnapping = false;
            LoadingOverlay.IsVisible = false;
            UpdateSnapToolbarText();
        }
    }

    private static string BuildManualSnapFailureMessage(string detail, bool hasCachedSnap)
    {
        var routeMessage = hasCachedSnap
            ? "Road snapping is currently unavailable. The previously snapped route is still shown."
            : "Road snapping is currently unavailable. The raw GPS route is still shown.";

        if (string.IsNullOrWhiteSpace(detail))
            return routeMessage;

        return $"{routeMessage}\n\n{detail}";
    }

    private async void DeleteToolbarItem_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return;

        var confirmed = await DisplayAlertAsync(
            AppText.RouteDetailDeleteTitle,
            AppText.RouteDetailDeleteMessage,
            AppText.RouteDetailDeleteButton,
            AppText.CommonCancel);

        if (!confirmed)
            return;

        await RouteStorage.DeleteAsync(SessionId);

        if (Preferences.Get("LastRouteSessionId", string.Empty) == SessionId)
            Preferences.Set("LastRouteSessionId", string.Empty);

        await Shell.Current.GoToAsync("..");
    }
}
