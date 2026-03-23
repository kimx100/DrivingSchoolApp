namespace DrivingSchoolApp.Models;

public sealed class RouteSnapState
{
    public string SessionId { get; set; } = string.Empty;
    public RouteSnapWorkStatus Status { get; set; } = RouteSnapWorkStatus.Pending;
    public bool HasBeenViewed { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}