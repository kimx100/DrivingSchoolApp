using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services.API;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingSchoolApp.Pages;

public partial class TheoryLessonPage : ContentPage
{
    private readonly ObservableCollection<StudentDto> _students = new();
    private readonly List<TheoryStudentAttendanceItem> _attendanceItems = new();
    private bool _isLoading;
    private bool _lessonStarted;
    private bool _instructorSigned;
    private int _currentStudentIndex;
    private LessonSignature? _instructorSignature;

    public TheoryLessonPage()
    {
        InitializeComponent();
        StudentsCollectionView.ItemsSource = _students;
        InstructorSignaturePad.Lines = new ObservableCollection<IDrawingLine>();
        StudentSignaturePad.Lines = new ObservableCollection<IDrawingLine>();
        InstructorSignaturePad.DrawingLineCompleted += SignaturePad_DrawingLineCompleted;
        StudentSignaturePad.DrawingLineCompleted += SignaturePad_DrawingLineCompleted;
        UpdateTheoryLessonView();
        UpdateSignatureStatus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_lessonStarted)
            await LoadStudentsAsync();
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e)
    {
        await LoadStudentsAsync();
    }

    private void StartLessonButton_Clicked(object? sender, EventArgs e)
    {
        if (_students.Count == 0)
        {
            ShowStatus("Load students before starting a theory lesson.");
            return;
        }

        _attendanceItems.Clear();
        foreach (var student in OrderStudents(_students))
            _attendanceItems.Add(new TheoryStudentAttendanceItem(student));

        _lessonStarted = true;
        _instructorSigned = false;
        _currentStudentIndex = 0;
        _instructorSignature = null;
        InstructorSignaturePad.Clear();
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();

        ShowStatus(string.Empty);
        UpdateTheoryLessonView();
    }

    private async void SaveInstructorSignatureButton_Clicked(object? sender, EventArgs e)
    {
        if (!HasSignature(InstructorSignaturePad))
        {
            await DisplayAlertAsync("Signature required", "Instructor signature is required before continuing.", "OK");
            UpdateSignatureStatus();
            return;
        }

        _instructorSignature = BuildSignature(InstructorSignaturePad, "Instructor", DateTimeOffset.UtcNow);
        _instructorSigned = true;
        _currentStudentIndex = 0;
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();
        UpdateTheoryLessonView();
    }

    private void ClearInstructorSignatureButton_Clicked(object? sender, EventArgs e)
    {
        InstructorSignaturePad.Clear();
        _instructorSignature = null;
        _instructorSigned = false;
        UpdateSignatureStatus();
    }

    private async void SaveStudentSignatureButton_Clicked(object? sender, EventArgs e)
    {
        var current = GetCurrentAttendanceItem();
        if (current is null)
            return;

        if (!HasSignature(StudentSignaturePad))
        {
            await DisplayAlertAsync("Signature required", "Student signature is required before saving.", "OK");
            UpdateSignatureStatus();
            return;
        }

        current.Signature = BuildSignature(StudentSignaturePad, FormatStudentName(current.Student), DateTimeOffset.UtcNow);
        current.HasSigned = true;
        current.IsSkipped = false;
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();
        MoveToNextStudent();
    }

    private void ClearStudentSignatureButton_Clicked(object? sender, EventArgs e)
    {
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();
    }

    private void SkipStudentButton_Clicked(object? sender, EventArgs e)
    {
        var current = GetCurrentAttendanceItem();
        if (current is null)
            return;

        current.HasSigned = false;
        current.IsSkipped = true;
        current.Signature = null;
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();
        MoveToNextStudent();
    }

    private void FinishButton_Clicked(object? sender, EventArgs e)
    {
        // TODO: Create theory lessons through the API when lesson upload is ready.
        // Expected future flow: capture instructor signature once, then POST one
        // theory lesson per signed student with that student's signature.
        _lessonStarted = false;
        _instructorSigned = false;
        _currentStudentIndex = 0;
        _instructorSignature = null;
        _attendanceItems.Clear();
        InstructorSignaturePad.Clear();
        StudentSignaturePad.Clear();
        UpdateSignatureStatus();

        ShowStatus("Theory lesson completed locally. Nothing was sent to the API.");
        UpdateTheoryLessonView();
    }

    private void SignaturePad_DrawingLineCompleted(object? sender, DrawingLineCompletedEventArgs e)
    {
        UpdateSignatureStatus();
    }

    private async Task LoadStudentsAsync()
    {
        if (_isLoading)
            return;

        SetLoadingState(true);

        try
        {
            _students.Clear();
            
            var services = Handler?.MauiContext?.Services
                           ?? throw new InvalidOperationException("Application services are not available.");

            var authService = services.GetRequiredService<IAuthService>();
            var drivingSchoolService = services.GetRequiredService<IDrivingSchoolService>();

            var instructorResult = await authService.GetSelfAsync<InstructorDto>();
            if (!instructorResult.IsSuccessful || instructorResult.Data is null)
                throw new InvalidOperationException("Could not load the current instructor.");

            var studentsResult = await drivingSchoolService.GetAllStudentsFromSchoolAsync(instructorResult.Data.SchoolId);
            if (!studentsResult.IsSuccessful)
                throw new InvalidOperationException("Could not load students from the driving school.");

            foreach (var student in OrderStudents(studentsResult.Data ?? []))
                _students.Add(student);

            ShowStatus(_students.Count == 0 ? "No students found." : string.Empty);
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        _isLoading = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        RefreshButton.IsEnabled = !isLoading;
        StartLessonButton.IsEnabled = !isLoading && !_lessonStarted && _students.Count > 0;
    }

    private void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private void UpdateTheoryLessonView()
    {
        StudentsCollectionView.IsVisible = !_lessonStarted;
        LessonFlowView.IsVisible = _lessonStarted;
        RefreshButton.IsVisible = !_lessonStarted;
        StartLessonButton.IsVisible = !_lessonStarted;
        StartLessonButton.IsEnabled = !_isLoading && _students.Count > 0;

        if (!_lessonStarted)
            return;

        if (!_instructorSigned)
        {
            StepTitleLabel.Text = "Step 1: Instructor signature";
            StepStatusLabel.Text = "Instructor signature required";
            InstructorStepCard.IsVisible = true;
            StudentStepCard.IsVisible = false;
            SummaryStepCard.IsVisible = false;
            return;
        }

        var current = GetCurrentAttendanceItem();
        if (current is not null)
        {
            var student = current.Student;
            StepTitleLabel.Text = "Step 2: Student signatures";
            StepStatusLabel.Text = $"Student {_currentStudentIndex + 1} of {_attendanceItems.Count}";
            CurrentStudentNameLabel.Text = FormatStudentName(student);
            CurrentStudentEmailLabel.Text = student.EmailAddress;
            CurrentStudentPhoneLabel.Text = student.PhoneNumber;

            InstructorStepCard.IsVisible = false;
            StudentStepCard.IsVisible = true;
            SummaryStepCard.IsVisible = false;
            return;
        }

        StepTitleLabel.Text = "Step 3: Summary";
        StepStatusLabel.Text = "Theory lesson attendance is complete.";
        TotalStudentsLabel.Text = $"Total students: {_attendanceItems.Count}";
        SignedStudentsLabel.Text = $"Signed students: {_attendanceItems.Count(x => x.HasSigned)}";
        SkippedStudentsLabel.Text = $"Skipped students: {_attendanceItems.Count(x => x.IsSkipped)}";
        InstructorSignatureCapturedLabel.Text = $"Instructor signature captured: {(_instructorSignature is not null ? "Yes" : "No")}";

        InstructorStepCard.IsVisible = false;
        StudentStepCard.IsVisible = false;
        SummaryStepCard.IsVisible = true;
    }

    private TheoryStudentAttendanceItem? GetCurrentAttendanceItem()
    {
        if (_currentStudentIndex < 0 || _currentStudentIndex >= _attendanceItems.Count)
            return null;

        return _attendanceItems[_currentStudentIndex];
    }

    private void MoveToNextStudent()
    {
        _currentStudentIndex++;
        UpdateTheoryLessonView();
    }

    private static List<StudentDto> OrderStudents(IEnumerable<StudentDto> students)
    {
        return students
            .OrderBy(x => x.StudentName.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.StudentName.LastName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatStudentName(StudentDto student)
        => $"{student.StudentName.FirstName} {student.StudentName.LastName}".Trim();

    private void UpdateSignatureStatus()
    {
        InstructorSignatureStatusLabel.Text = HasSignature(InstructorSignaturePad)
            ? "Instructor signature captured."
            : "Draw instructor signature above.";

        StudentSignatureStatusLabel.Text = HasSignature(StudentSignaturePad)
            ? "Student signature captured."
            : "Draw student signature above.";
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
}

internal sealed class TheoryStudentAttendanceItem(StudentDto student)
{
    public StudentDto Student { get; } = student;
    public bool HasSigned { get; set; }
    public bool IsSkipped { get; set; }
    public LessonSignature? Signature { get; set; }
}
