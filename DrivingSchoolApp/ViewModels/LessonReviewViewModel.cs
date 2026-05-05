using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;

namespace DrivingSchoolApp.ViewModels;

public sealed class LessonReviewViewModel : INotifyPropertyChanged
{
    private RouteSession? _session;
    private string _studentName = string.Empty;
    private string _instructorName = string.Empty;
    private string _startText = string.Empty;
    private string _endText = string.Empty;
    private string _durationText = string.Empty;
    private string _distanceText = string.Empty;
    private string _studentProgressText = AppText.StudentProgressPrompt;
    private bool _showNoRemainingItemsMessage;

    public ObservableCollection<LessonItemSelectionViewModel> LessonItems { get; } = new();

    public string StudentName
    {
        get => _studentName;
        set => SetProperty(ref _studentName, value);
    }

    public string InstructorName
    {
        get => _instructorName;
        set => SetProperty(ref _instructorName, value);
    }

    public string StartText
    {
        get => _startText;
        private set => SetProperty(ref _startText, value);
    }

    public string EndText
    {
        get => _endText;
        private set => SetProperty(ref _endText, value);
    }

    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public string DistanceText
    {
        get => _distanceText;
        private set => SetProperty(ref _distanceText, value);
    }

    public string StudentProgressText
    {
        get => _studentProgressText;
        private set => SetProperty(ref _studentProgressText, value);
    }

    public bool ShowNoRemainingItemsMessage
    {
        get => _showNoRemainingItemsMessage;
        private set => SetProperty(ref _showNoRemainingItemsMessage, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplySession(RouteSession session, string defaultStudentName, string defaultInstructorName)
    {
        _session = session;

        StudentName = string.IsNullOrWhiteSpace(session.StudentName)
            ? defaultStudentName
            : session.StudentName;

        InstructorName = string.IsNullOrWhiteSpace(session.InstructorName)
            ? defaultInstructorName
            : session.InstructorName;

        StartText = session.StartedAt.LocalDateTime.ToString("dd MMM yyyy HH:mm");
        EndText = session.EndedAt.LocalDateTime.ToString("dd MMM yyyy HH:mm");
        DurationText = RouteSessionMetrics.FormatDuration(session.EndedAt - session.StartedAt);
        DistanceText = RouteSessionMetrics.FormatDistance(
            session.TotalDistanceMeters ?? RouteSessionMetrics.CalculateDistanceMeters(session.Points));
    }

    public async Task RefreshStudentProgressAsync()
    {
        var completedTypes = new HashSet<LessonItemType>();
        var selectedTypes = LessonItems
            .Where(x => x.IsSelected)
            .Select(x => x.ItemType)
            .ToHashSet();

        StudentRecord? student = null;

        if (!string.IsNullOrWhiteSpace(StudentName))
            student = await StudentProgressStorage.FindByNameAsync(StudentName);

        if (student is null && _session is not null)
            student = await StudentProgressStorage.GetByIdAsync(_session.StudentId);

        if (student is not null)
        {
            foreach (var item in student.CompletedItems)
                completedTypes.Add(item.ItemType);
        }

        LessonItems.Clear();

        foreach (var definition in LessonCatalogService.GetAll())
        {
            if (completedTypes.Contains(definition.ItemType))
                continue;

            LessonItems.Add(new LessonItemSelectionViewModel(
                definition.ItemType,
                definition.Key,
                definition.DisplayName,
                definition.DisplayNameDa,
                definition.Description,
                selectedTypes.Contains(definition.ItemType)));
        }

        if (student is null)
        {
            StudentProgressText = string.IsNullOrWhiteSpace(StudentName)
                ? AppText.StudentProgressPrompt
                : AppText.StudentProgressNoHistory;
        }
        else if (student.CompletedItems.Count == 0)
        {
            StudentProgressText = AppText.StudentProgressEmpty;
        }
        else
        {
            StudentProgressText =
                AppText.StudentProgressCompletedPrefix +
                string.Join(", ",
                    student.CompletedItems
                        .OrderBy(x => x.CompletedAt)
                        .Select(x => ToDisplayName(x.ItemType)));
        }

        ShowNoRemainingItemsMessage = LessonItems.Count == 0;
    }

    public IReadOnlyList<LessonItemType> GetSelectedItemTypes()
        => LessonItems
            .Where(x => x.IsSelected)
            .Select(x => x.ItemType)
            .ToList();

    private static string ToDisplayName(LessonItemType itemType)
    {
        var definition = LessonCatalogService.GetAll().First(x => x.ItemType == itemType);
        return definition.DisplayNameDa ?? definition.DisplayName;
    }

    private bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class LessonItemSelectionViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public LessonItemSelectionViewModel(
        LessonItemType itemType,
        string key,
        string displayName,
        string? displayNameDa,
        string? description,
        bool isSelected)
    {
        ItemType = itemType;
        Key = key;
        DisplayName = displayName;
        DisplayNameDa = displayNameDa;
        Description = description;
        _isSelected = isSelected;
    }

    public LessonItemType ItemType { get; }
    public string Key { get; }
    public string DisplayName { get; }
    public string? DisplayNameDa { get; }
    public string? Description { get; }
    public string DisplayText => DisplayNameDa ?? DisplayName;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
