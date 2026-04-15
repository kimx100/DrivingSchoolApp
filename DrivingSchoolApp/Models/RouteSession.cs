using System;
using System.Collections.Generic;

namespace DrivingSchoolApp.Models;

public sealed class RouteSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public string? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public double? TotalDistanceMeters { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public List<CompletedLessonItem> CompletedItems { get; set; } = new();
    public LessonSignature? StudentSignature { get; set; }
    public LessonSignature? InstructorSignature { get; set; }
    public List<TrackPoint> Points { get; set; } = new();
}
