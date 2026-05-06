using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Mapper;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.Services.API;
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
    private readonly IAuthService _authService;
    private readonly IInstructorService _instructorService;
    private readonly IDrivingSchoolService _drivingSchoolService;
    
    private const string CurrentStudentNameKey = "CurrentStudentName";
    private const string CurrentInstructorNameKey = "CurrentInstructorName";

    private readonly LessonReviewViewModel _viewModel = new();
    private readonly ObservableCollection<RouteStudentPickerItem> _studentPickerItems = new();

    private bool _mapInitialized;
    private string? _sessionId;
    private string? _loadedSessionId;
    private RouteSession? _session;
    private InstructorDto? _instructor;
    private RouteStudentPickerItem? _selectedStudent;

    public string? SessionId
    {
        get => _sessionId;
        set => _sessionId = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public RouteConfirmationPage(
        IAuthService authService, 
        IInstructorService instructorService,
        IDrivingSchoolService drivingSchoolService)
    {
        InitializeComponent();

        _authService = authService;
        _instructorService = instructorService;
        _drivingSchoolService = drivingSchoolService;
        
        BindingContext = _viewModel;
        StudentPicker.ItemsSource = _studentPickerItems;

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
            string.Empty);

        await LoadInstructorAndStudentsAsync();
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
            Position = ordered.First(),
            Scale = RouteMapViewportHelper.RouteEndpointPinScale
        });

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailEndPin,
            Position = ordered.Last(),
            Scale = RouteMapViewportHelper.RouteEndpointPinScale
        });

        ZoomToPositions(ordered);
    }

    private void ZoomToPositions(IReadOnlyList<Position> positions)
    {
        RouteMapViewportHelper.FitRoute(
            RouteMapView,
            positions,
            debugSource: "RouteConfirmationPage Raw");
    }

    private async void StudentPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedStudent = StudentPicker.SelectedItem as RouteStudentPickerItem;

        if (_selectedStudent is not null)
            _viewModel.StudentName = _selectedStudent.FullName;

        await _viewModel.RefreshStudentProgressAsync();
    }

    private async Task LoadInstructorAndStudentsAsync()
    {
        PeopleStatusLabel.Text = "Loading instructor and students...";
        InstructorNameLabel.Text = "Loading instructor...";
        StudentPicker.IsEnabled = false;
        _studentPickerItems.Clear();
        _instructor = null;
        _selectedStudent = null;

        try
        {
            var instructorResult = await _authService.GetSelfAsync<InstructorDto>();
            
            _instructor = instructorResult.Data!;
            var instructorName = FormatInstructorName(_instructor);
            
            _viewModel.InstructorName = instructorName;
            InstructorNameLabel.Text = $"Instructor: {instructorName}";

            var studentsResult = await _drivingSchoolService.GetAllStudentsFromSchoolAsync(_instructor.SchoolId);
            var students = studentsResult.Data!;
            
            foreach (var student in OrderStudents(students))
                _studentPickerItems.Add(new RouteStudentPickerItem(student));

            SelectInitialStudent();

            StudentPicker.IsEnabled = _studentPickerItems.Count > 0;
            PeopleStatusLabel.Text = _studentPickerItems.Count == 0
                ? "No students found for this instructor's driving school."
                : "Select the student who completed this route.";
        }
        catch (Exception ex)
        {
            _viewModel.InstructorName = string.Empty;
            InstructorNameLabel.Text = "Instructor: Not loaded";
            PeopleStatusLabel.Text = BuildPeopleLoadError(ex);
            StudentPicker.IsEnabled = false;
        }
    }

    private void SelectInitialStudent()
    {
        if (_studentPickerItems.Count == 0)
            return;

        RouteStudentPickerItem? match = null;

        if (!string.IsNullOrWhiteSpace(_session?.StudentId))
        {
            match = _studentPickerItems.FirstOrDefault(x =>
                string.Equals(x.Student.Id.ToString(), _session.StudentId, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null && !string.IsNullOrWhiteSpace(_viewModel.StudentName))
        {
            match = _studentPickerItems.FirstOrDefault(x =>
                string.Equals(x.FullName, _viewModel.StudentName, StringComparison.OrdinalIgnoreCase));
        }

        if (match is not null)
            StudentPicker.SelectedItem = match;
    }

    private static string BuildPeopleLoadError(Exception exception)
    {
        if (exception is InvalidOperationException)
            return exception.Message;

#if DEBUG
        return $"Could not load instructor/students. {exception.GetType().Name}: {exception.Message}";
#else
        return "Could not load instructor/students. Check that you are logged in and the API is reachable.";
#endif
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
        
        var self = await _authService.GetSelfAsync<InstructorDto>();
        if (!self.IsSuccessful)
        {
            await DisplayAlertAsync("Unauthorized", "You need to log in before you can save a driving route",
                AppText.CommonOk);
            await Shell.Current.GoToAsync($"//{nameof(LogInPage)}");
            return;
        }

        var selectedStudent = _selectedStudent;
        var studentName = selectedStudent?.FullName ?? string.Empty;
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

        await StudentProgressStorage.SaveLessonCompletionAsync(
            studentName,
            selectedStudent.Student.Id.ToString(),
            _session.Id,
            selectedItems,
            finalizedAt);

        // TODO: Use instructor/student API ids when uploading finalized routes to the API.
        _session.StudentId = selectedStudent.Student.Id.ToString();
        _session.StudentName = studentName;
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
        _session.StudentSignature = BuildSignature(StudentSignaturePad, studentName, finalizedAt);
        _session.IsFinalized = true;
        _session.FinalizedAt = finalizedAt;
        
        var drivingLessonRegistry = await _session.ToRegistryDto(
            self.Data!.SchoolId,
            new Money(1000, "DKK"), // TODO: Money should come from user input
            (int)InstructorSignaturePad.Width,
            (int)InstructorSignaturePad.Height
        );

        var created = await _instructorService.CreateDrivingLessonAsync(self.Data!.Id, drivingLessonRegistry);
        if (!created.IsSuccessful)
        {
            await DisplayAlertAsync("Error", created.ErrorMessage, AppText.CommonOk);
            return;
        }
        
        await RouteStorage.SaveAsync(_session);
        await RouteSnapBackgroundProcessor.EnqueueAsync(_session.Id);

        Preferences.Set(CurrentStudentNameKey, studentName);
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

    private static List<StudentDto> OrderStudents(IEnumerable<StudentDto> students)
    {
        return students
            .OrderBy(x => x.StudentName.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.StudentName.LastName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatInstructorName(InstructorDto instructor)
        => $"{instructor.Name.FirstName} {instructor.Name.LastName}".Trim();
}

internal sealed class RouteStudentPickerItem(StudentDto student)
{
    public StudentDto Student { get; } = student;
    public string FullName { get; } = $"{student.StudentName.FirstName} {student.StudentName.LastName}".Trim();
}
