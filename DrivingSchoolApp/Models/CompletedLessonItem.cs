namespace DrivingSchoolApp.Models;

public sealed class CompletedLessonItem
{
    public LessonItemType ItemType { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public string? LessonSessionId { get; set; }
}
