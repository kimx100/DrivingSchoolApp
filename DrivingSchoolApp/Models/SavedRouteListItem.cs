using System.ComponentModel;
using System.Runtime.CompilerServices;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Services;
using Microsoft.Maui.Graphics;

namespace DrivingSchoolApp.Models;

public sealed class SavedRouteListItem : INotifyPropertyChanged
{
    private bool _isEditing;
    private bool _isSelected;
    private string _statusText = string.Empty;
    private Color _statusColor = Colors.Transparent;
    private bool _showStatus;
    private bool _canOpen = true;
    private double _rowOpacity = 1.0;

    public string SessionId { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset EndedAt { get; }
    public int PointCount { get; }
    public string StudentName { get; }
    public bool IsFinalized { get; }

    public string Title => StartedAt.LocalDateTime.ToString("dd MMM yyyy HH:mm");

    public string Subtitle
    {
        get
        {
            var duration = EndedAt - StartedAt;
            var studentPart = string.IsNullOrWhiteSpace(StudentName)
                ? AppText.SavedRouteNoStudentAssigned
                : StudentName;

            return AppText.FormatSavedRouteSubtitle(
                studentPart,
                RouteSessionMetrics.FormatDuration(duration),
                PointCount);
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetField(ref _isEditing, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public Color StatusColor
    {
        get => _statusColor;
        set => SetField(ref _statusColor, value);
    }

    public bool ShowStatus
    {
        get => _showStatus;
        set => SetField(ref _showStatus, value);
    }

    public bool CanOpen
    {
        get => _canOpen;
        set => SetField(ref _canOpen, value);
    }

    public double RowOpacity
    {
        get => _rowOpacity;
        set => SetField(ref _rowOpacity, value);
    }

    public SavedRouteListItem(
        string sessionId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        int pointCount,
        string studentName,
        bool isFinalized)
    {
        SessionId = sessionId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        PointCount = pointCount;
        StudentName = studentName;
        IsFinalized = isFinalized;
    }

    public static SavedRouteListItem FromSession(RouteSession session)
    {
        var pointCount = session.Points?.Count ?? 0;

        return new SavedRouteListItem(
            session.Id,
            session.StartedAt,
            session.EndedAt,
            pointCount,
            session.StudentName,
            session.IsFinalized);
    }

    public void ApplyStatus(RouteSnapState? state)
    {
        if (!IsFinalized)
        {
            ShowStatus = true;
            StatusText = AppText.SavedRouteStatusNeedsSignOff;
            StatusColor = Colors.DarkOrange;
            CanOpen = true;
            RowOpacity = 1.0;
            return;
        }

        if (state is null)
        {
            ShowStatus = false;
            StatusText = string.Empty;
            StatusColor = Colors.Transparent;
            CanOpen = true;
            RowOpacity = 1.0;
            return;
        }

        switch (state.Status)
        {
            case RouteSnapWorkStatus.Pending:
            case RouteSnapWorkStatus.Processing:
                ShowStatus = true;
                StatusText = AppText.SavedRouteStatusMapProcessing;
                StatusColor = Colors.Gray;
                CanOpen = true;
                RowOpacity = 1.0;
                break;

            case RouteSnapWorkStatus.Failed:
                ShowStatus = true;
                StatusText = AppText.SavedRouteStatusSnapFailed;
                StatusColor = Colors.Red;
                CanOpen = true;
                RowOpacity = 1.0;
                break;

            case RouteSnapWorkStatus.ReadyToView:
                ShowStatus = true;
                StatusText = AppText.SavedRouteStatusReadyToView;
                StatusColor = Color.FromArgb("#2F6FD6");
                CanOpen = true;
                RowOpacity = 1.0;
                break;

            case RouteSnapWorkStatus.Viewed:
            default:
                ShowStatus = false;
                StatusText = string.Empty;
                StatusColor = Colors.Transparent;
                CanOpen = true;
                RowOpacity = 1.0;
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
