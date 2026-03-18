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
    public bool IsPaused => HasActiveSession && !IsTracking;
    public bool HasFirstPoint => LatestPoint is not null;
}