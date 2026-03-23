namespace DrivingSchoolApp.Models;

public sealed class RouteSnapAttemptResult
{
    public bool Success { get; init; }
    public bool HadInternet { get; init; }
    public string Message { get; init; } = string.Empty;
    public SnappedRouteCache? Cache { get; init; }

    public static RouteSnapAttemptResult Ok(SnappedRouteCache cache) => new()
    {
        Success = true,
        HadInternet = true,
        Message = "Route snapped successfully.",
        Cache = cache
    };

    public static RouteSnapAttemptResult Fail(bool hadInternet, string message) => new()
    {
        Success = false,
        HadInternet = hadInternet,
        Message = message
    };
}