namespace DrivingSchoolApp.Models;

public sealed record TrackPoint(
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    double? SpeedMps
);