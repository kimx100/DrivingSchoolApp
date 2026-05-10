using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.Services.API;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingSchoolApp.Pages;

public partial class TheoryLessonPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IDrivingSchoolService _drivingSchoolService;
    
    private readonly ObservableCollection<StudentDto> _students = new();
    private readonly List<TheoryStudentAttendanceItem> _attendanceItems = new();
    private bool _isLoading;
    private bool _lessonStarted;
    private bool _instructorSigned;
    private bool _isUpdatingStudentPrice;
    private int _currentStudentIndex;
    private decimal _theoryLessonPrice;
    private LessonSignature? _instructorSignature;
    private DrawingView? _instructorSignaturePad;
    private DrawingView? _studentSignaturePad;

    private DrawingView InstructorSignaturePad
        => _instructorSignaturePad ??= CreateSignaturePad(InstructorSignatureHost, "instructor");

    private DrawingView StudentSignaturePad
        => _studentSignaturePad ??= CreateSignaturePad(StudentSignatureHost, "student");

    public TheoryLessonPage(IAuthService authService, IDrivingSchoolService drivingSchoolService)
    {
        _authService = authService;
        _drivingSchoolService = drivingSchoolService;
        
        InitializeComponent();
        _theoryLessonPrice = LessonPriceSettingsService.GetDefaultTheoryLessonPrice();
        TheoryLessonPriceEntry.Text = LessonPriceSettingsService.FormatPrice(_theoryLessonPrice);
        StudentsCollectionView.ItemsSource = _students;
        UpdateTheoryLessonView();
        UpdateSignatureStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_lessonStarted)
        {
            _theoryLessonPrice = LessonPriceSettingsService.GetDefaultTheoryLessonPrice();
            TheoryLessonPriceEntry.Text = LessonPriceSettingsService.FormatPrice(_theoryLessonPrice);
        }

        if (!_lessonStarted)
            Dispatcher.Dispatch(async () => await LoadStudentsAsync());
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e)
    {
        await LoadStudentsAsync(false);
    }

    private void StartLessonButton_Clicked(object? sender, EventArgs e)
    {
        TheoryLessonPriceEntry.Unfocus();

        if (_students.Count == 0)
        {
            ShowStatus("Load students before starting a theory lesson.");
            return;
        }

        if (!TryReadTheoryLessonPrice(out var lessonPrice))
            return;

        _theoryLessonPrice = lessonPrice;

        _attendanceItems.Clear();
        foreach (var student in OrderStudents(_students))
            _attendanceItems.Add(new TheoryStudentAttendanceItem(student, _theoryLessonPrice));

        _lessonStarted = true;
        _instructorSigned = false;
        _currentStudentIndex = 0;
        _instructorSignature = null;
        ResetSignaturePads();
        UpdateSignatureStatus();

        ShowStatus(string.Empty);
        UpdateTheoryLessonView();
    }

    private void TheoryLessonPriceEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!LessonPriceSettingsService.TryParsePrice(TheoryLessonPriceEntry.Text, out var price))
            return;

        _theoryLessonPrice = price;

        if (!_lessonStarted)
            return;

        foreach (var item in _attendanceItems.Where(x => !x.HasSigned && !x.IsSkipped && !x.IsPriceCustomized))
            item.PriceAmount = price;

        var current = GetCurrentAttendanceItem();
        if (current is not null && !current.IsPriceCustomized)
            SetCurrentStudentPriceText(current.PriceAmount);
    }

    private void CurrentStudentPriceEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingStudentPrice)
            return;

        var current = GetCurrentAttendanceItem();
        if (current is null)
            return;

        if (!LessonPriceSettingsService.TryParsePrice(CurrentStudentPriceEntry.Text, out var price))
            return;

        current.PriceAmount = price;
        current.IsPriceCustomized = true;
    }

    private async void SaveInstructorSignatureButton_Clicked(object? sender, EventArgs e)
    {
        TheoryLessonPriceEntry.Unfocus();

        if (!HasSignature(InstructorSignaturePad))
        {
            await DisplayAlertAsync("Signature required", "Instructor signature is required before continuing.", "OK");
            UpdateSignatureStatus();
            return;
        }

        _instructorSignature = BuildSignature(InstructorSignaturePad, "Instructor", DateTimeOffset.UtcNow);
        _instructorSigned = true;
        _currentStudentIndex = 0;
        ClearSignaturePad(_studentSignaturePad);
        UpdateSignatureStatus();
        UpdateTheoryLessonView();
    }

    private void ClearInstructorSignatureButton_Clicked(object? sender, EventArgs e)
    {
        SignatureDrawingViewHelper.Clear(InstructorSignaturePad);
        _instructorSignature = null;
        _instructorSigned = false;
        UpdateSignatureStatus();
    }

    private async void SaveStudentSignatureButton_Clicked(object? sender, EventArgs e)
    {
        CurrentStudentPriceEntry.Unfocus();

        var current = GetCurrentAttendanceItem();
        if (current is null)
            return;

        if (!HasSignature(StudentSignaturePad))
        {
            await DisplayAlertAsync("Signature required", "Student signature is required before saving.", "OK");
            UpdateSignatureStatus();
            return;
        }

        if (!LessonPriceSettingsService.TryParsePrice(CurrentStudentPriceEntry.Text, out var studentPrice))
        {
            await DisplayAlertAsync("Ugyldig pris", "Angiv en gyldig pris for eleven.", "OK");
            return;
        }

        current.PriceAmount = studentPrice;
        current.Signature = BuildSignature(StudentSignaturePad, FormatStudentName(current.Student), DateTimeOffset.UtcNow);
        current.HasSigned = true;
        current.IsSkipped = false;
        SignatureDrawingViewHelper.Clear(StudentSignaturePad);
        UpdateSignatureStatus();
        MoveToNextStudent();
    }

    private void ClearStudentSignatureButton_Clicked(object? sender, EventArgs e)
    {
        SignatureDrawingViewHelper.Clear(StudentSignaturePad);
        UpdateSignatureStatus();
    }

    private void SkipStudentButton_Clicked(object? sender, EventArgs e)
    {
        CurrentStudentPriceEntry.Unfocus();

        var current = GetCurrentAttendanceItem();
        if (current is null)
            return;

        current.HasSigned = false;
        current.IsSkipped = true;
        current.Signature = null;
        SignatureDrawingViewHelper.Clear(StudentSignaturePad);
        UpdateSignatureStatus();
        MoveToNextStudent();
    }

    private void FinishButton_Clicked(object? sender, EventArgs e)
    {
        // TODO: Create theory lessons through the API when lesson upload is ready.
        // Expected future flow: capture instructor signature once, then POST one
        // theory lesson per signed student with that student's signature.
        ResetActiveTheoryLesson();

        ShowStatus("Theory lesson completed locally. Nothing was sent to the API.");
        UpdateTheoryLessonView();
    }

    private async void CancelLessonButton_Clicked(object? sender, EventArgs e)
    {
        TheoryLessonPriceEntry.Unfocus();
        CurrentStudentPriceEntry.Unfocus();

        if (HasTheoryLessonProgress())
        {
            var confirmed = await DisplayAlertAsync(
                "Annuller lektion",
                "Vil du annullere lektionen? Signaturer og ændringer for denne igangværende lektion bliver ikke gemt.",
                "Annuller lektion",
                "Fortsæt lektion");

            if (!confirmed)
                return;
        }

        ResetActiveTheoryLesson();
        ShowStatus("Lektion annulleret.");
        UpdateTheoryLessonView();
    }

    private void SignaturePad_DrawingLineCompleted(object? sender, DrawingLineCompletedEventArgs e)
    {
        UpdateSignatureStatus();
    }

    private void TheoryLessonPriceEntry_Completed(object? sender, EventArgs e)
    {
        TheoryLessonPriceEntry.Unfocus();
    }

    private void CurrentStudentPriceEntry_Completed(object? sender, EventArgs e)
    {
        CurrentStudentPriceEntry.Unfocus();
    }

    private async Task LoadStudentsAsync(bool checkCache = true)
    {
        if (_isLoading)
            return;

        SetLoadingState(true);

        try
        {
            _students.Clear();
            
            var instructorResult = await _authService.GetSelfAsync<InstructorDto>();
            if (!instructorResult.IsSuccessful || instructorResult.Data is null)
                throw new InvalidOperationException("Could not load the current instructor.");

            var studentsResult = await _drivingSchoolService.GetAllStudentsFromSchoolAsync(instructorResult.Data.SchoolId, checkCache);
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
        TheoryLessonPricePanel.IsVisible = !_lessonStarted;
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
            PrepareInstructorSignaturePad();
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
            SetCurrentStudentPriceText(current.PriceAmount);

            InstructorStepCard.IsVisible = false;
            StudentStepCard.IsVisible = true;
            SummaryStepCard.IsVisible = false;
            PrepareStudentSignaturePad();
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
        RefreshSignatureLayout();
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

    private bool TryReadTheoryLessonPrice(out decimal price)
    {
        if (LessonPriceSettingsService.TryParsePrice(TheoryLessonPriceEntry.Text, out price))
            return true;

        ShowStatus("Angiv en gyldig pris for teorilektionen.");
        return false;
    }

    private void SetCurrentStudentPriceText(decimal price)
    {
        _isUpdatingStudentPrice = true;
        CurrentStudentPriceEntry.Text = LessonPriceSettingsService.FormatPrice(price);
        _isUpdatingStudentPrice = false;
    }

    private bool HasTheoryLessonProgress()
    {
        return _instructorSigned
            || _instructorSignature is not null
            || _currentStudentIndex > 0
            || HasSignature(_instructorSignaturePad)
            || HasSignature(_studentSignaturePad)
            || _attendanceItems.Any(x =>
                x.HasSigned ||
                x.IsSkipped ||
                x.Signature is not null ||
                x.IsPriceCustomized);
    }

    private void ResetActiveTheoryLesson()
    {
        _lessonStarted = false;
        _instructorSigned = false;
        _currentStudentIndex = 0;
        _instructorSignature = null;
        _attendanceItems.Clear();
        ResetSignaturePads();
        UpdateSignatureStatus();
    }

    private void ResetSignaturePads()
    {
        DestroySignaturePad(ref _instructorSignaturePad, InstructorSignatureHost);
        DestroySignaturePad(ref _studentSignaturePad, StudentSignatureHost);
    }

    private DrawingView CreateSignaturePad(ContentView host, string debugName)
    {
        var signaturePad = new DrawingView
        {
            HeightRequest = 180,
            InputTransparent = false,
            IsEnabled = true,
            BackgroundColor = Colors.White,
            IsMultiLineModeEnabled = true,
            ShouldClearOnFinish = false,
            LineColor = Colors.Black,
            LineWidth = 4
        };

        SignatureDrawingViewHelper.EnsureDrawable(signaturePad);
        signaturePad.DrawingLineCompleted += SignaturePad_DrawingLineCompleted;
        host.Content = signaturePad;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Theory {debugName} signature pad created.");
#endif

        return signaturePad;
    }

    private void PrepareInstructorSignaturePad()
    {
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(100);
            var signaturePad = InstructorSignaturePad;
            SignatureDrawingViewHelper.EnsureDrawable(signaturePad);
            LogSignaturePadState("instructor", signaturePad);
            RefreshSignatureLayout();
        });
    }

    private void PrepareStudentSignaturePad()
    {
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(100);
            var signaturePad = StudentSignaturePad;
            SignatureDrawingViewHelper.EnsureDrawable(signaturePad);
            LogSignaturePadState("student", signaturePad);
            RefreshSignatureLayout();
        });
    }

    private static void ClearSignaturePad(DrawingView? signaturePad)
    {
        if (signaturePad is not null)
            SignatureDrawingViewHelper.Clear(signaturePad);
    }

    private void DestroySignaturePad(ref DrawingView? signaturePad, ContentView host)
    {
        if (signaturePad is not null)
        {
            signaturePad.DrawingLineCompleted -= SignaturePad_DrawingLineCompleted;
            SignatureDrawingViewHelper.Clear(signaturePad);
        }

        signaturePad = null;
        host.Content = null;
    }

    private static void LogSignaturePadState(string name, DrawingView signaturePad)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"Theory {name} signature pad visible={signaturePad.IsVisible}, enabled={signaturePad.IsEnabled}, inputTransparent={signaturePad.InputTransparent}, lines={signaturePad.Lines?.Count ?? 0}, handler={(signaturePad.Handler is null ? "null" : "ready")}");
#endif
    }

    private void RefreshSignatureLayout()
    {
        Dispatcher.Dispatch(() =>
        {
            _instructorSignaturePad?.InvalidateMeasure();
            _studentSignaturePad?.InvalidateMeasure();
            InstructorStepCard.InvalidateMeasure();
            StudentStepCard.InvalidateMeasure();
            LessonFlowView.InvalidateMeasure();
            ForceLayout();
        });
    }

    private void UpdateSignatureStatus()
    {
        InstructorSignatureStatusLabel.Text = HasSignature(_instructorSignaturePad)
            ? "Instructor signature captured."
            : "Draw instructor signature above.";

        StudentSignatureStatusLabel.Text = HasSignature(_studentSignaturePad)
            ? "Student signature captured."
            : "Draw student signature above.";
    }

    private static bool HasSignature(DrawingView? signaturePad)
        => signaturePad?.Lines?.Any(x => x.Points?.Count > 1) == true;

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

internal sealed class TheoryStudentAttendanceItem(StudentDto student, decimal priceAmount)
{
    public StudentDto Student { get; } = student;
    public decimal PriceAmount { get; set; } = priceAmount;
    public bool IsPriceCustomized { get; set; }
    public bool HasSigned { get; set; }
    public bool IsSkipped { get; set; }
    public LessonSignature? Signature { get; set; }
}
