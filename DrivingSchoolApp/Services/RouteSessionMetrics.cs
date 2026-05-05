using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class RouteSessionMetrics
{
    public static double CalculateDistanceMeters(IReadOnlyList<TrackPoint>? points)
    {
        if (points is null || points.Count < 2)
            return 0;

        double total = 0;

        for (var i = 1; i < points.Count; i++)
        {
            total += HaversineMeters(
                points[i - 1].Latitude,
                points[i - 1].Longitude,
                points[i].Latitude,
                points[i].Longitude);
        }

        return total;
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        return duration.TotalHours >= 1
            ? duration.ToString(@"hh\:mm\:ss")
            : duration.ToString(@"mm\:ss");
    }

    public static string FormatDistance(double? meters)
    {
        if (meters is null)
            return "Not available";

        if (meters.Value >= 1000)
            return $"{meters.Value / 1000:0.0} km";

        return $"{meters.Value:0} m";
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371000.0;

        static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radius * c;
    }
}
