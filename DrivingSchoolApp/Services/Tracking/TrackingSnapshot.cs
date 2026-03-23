using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services.Tracking;

public sealed record TrackingSnapshot(
    bool IsTracking,
    bool HasActiveSession,
    DateTimeOffset? SessionStartedAt,
    TimeSpan Elapsed,
    int TotalPointCount,
    TrackPoint? LatestPoint)
{
    // Compatibility for older pages that still compile in the project
    public DateTimeOffset? StartedAt => SessionStartedAt;

    public bool IsPaused => HasActiveSession && !IsTracking;
    public bool HasFirstPoint => LatestPoint is not null;
}