using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;

namespace DrivingSchoolApp.Pages;

public partial class TheoryLessonPage : ContentPage
{
#if DEBUG
    // -----------------------------------------------------------------------------
    // DEBUG PLACEHOLDER STUDENTS
    // Temporary data for debugging the Theory page before real login/API
    // is connected. Set UsePlaceholderStudents to true to force placeholder data.
    // -----------------------------------------------------------------------------
    private const bool UsePlaceholderStudents = false;

    private static readonly List<StudentDto> PlaceholderStudents = new()
    {
        new(
            Guid.Parse("bb4a35a8-4f85-47e3-b021-0cd0c971e3c4"),
            Guid.Parse("96a2174c-7037-4ab9-b45b-7864fbced9ce"),
            new NameDto("Emma", "Jensen"),
            "emma.jensen@example.test",
            "+45 22 14 63 90"),
        new(
            Guid.Parse("6fc78b5e-6f3c-43ea-9e79-e64f04093843"),
            Guid.Parse("96a2174c-7037-4ab9-b45b-7864fbced9ce"),
            new NameDto("Noah", "Madsen"),
            "noah.madsen@example.test",
            "+45 31 82 47 05"),
        new(
            Guid.Parse("6c619aa1-4fdc-4af0-b111-d949f4625ed9"),
            Guid.Parse("96a2174c-7037-4ab9-b45b-7864fbced9ce"),
            new NameDto("Sofia", "Larsen"),
            "sofia.larsen@example.test",
            "+45 42 71 15 38"),
        new(
            Guid.Parse("7c780610-28d0-49a3-97b1-9b6843252d12"),
            Guid.Parse("96a2174c-7037-4ab9-b45b-7864fbced9ce"),
            new NameDto("Lucas", "Nielsen"),
            "lucas.nielsen@example.test",
            "+45 53 64 28 11"),
        new(
            Guid.Parse("dfd6628c-d90a-4d4a-8a24-ad1da2f5d415"),
            Guid.Parse("96a2174c-7037-4ab9-b45b-7864fbced9ce"),
            new NameDto("Freja", "Andersen"),
            "freja.andersen@example.test",
            "+45 61 92 74 36")
    };

    private static bool ShouldUsePlaceholderStudents()
        => UsePlaceholderStudents;
#endif

    private readonly ObservableCollection<StudentDto> _students = new();
    private readonly TheoryLessonStudentService _studentService = new();
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

#if DEBUG
            if (ShouldUsePlaceholderStudents())
            {
                foreach (var student in OrderStudents(PlaceholderStudents))
                    _students.Add(student);

                ShowStatus("Showing placeholder students for debug.");
                return;
            }
#endif

            var students = await _studentService.GetStudentsForCurrentInstructorAsync();

            foreach (var student in OrderStudents(students))
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
            .OrderBy(x => x.StudentName.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.StudentName.FirstName, StringComparer.OrdinalIgnoreCase)
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
