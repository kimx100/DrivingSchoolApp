namespace DrivingSchoolApp.Models;

public sealed class StudentRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLessonEndedAt { get; set; }
    public List<CompletedLessonItem> CompletedItems { get; set; } = new();
}
