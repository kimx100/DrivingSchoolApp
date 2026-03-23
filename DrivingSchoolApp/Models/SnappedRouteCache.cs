namespace DrivingSchoolApp.Models;

public sealed class SnappedRouteCache
{
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int SourcePointCount { get; set; }
    public List<RouteCoordinate> Geometry { get; set; } = new();
}