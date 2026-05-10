using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;

namespace DrivingSchoolApp.Tests.Services;

public class RouteSessionMetricsTests
{
    [Fact]
    public void CalculateDistanceMeters_WithNoPoints_ReturnsZero()
    {
        Assert.Equal(0, RouteSessionMetrics.CalculateDistanceMeters(Array.Empty<TrackPoint>()));
        Assert.Equal(0, RouteSessionMetrics.CalculateDistanceMeters(null));
    }

    [Fact]
    public void CalculateDistanceMeters_WithOnePoint_ReturnsZero()
    {
        var points = new[]
        {
            CreatePoint(55.6761, 12.5683)
        };

        Assert.Equal(0, RouteSessionMetrics.CalculateDistanceMeters(points));
    }

    [Fact]
    public void CalculateDistanceMeters_WithTwoNearbyPoints_ReturnsReasonableDistance()
    {
        var points = new[]
        {
            CreatePoint(55.6761, 12.5683),
            CreatePoint(55.6761, 12.5693)
        };

        var distance = RouteSessionMetrics.CalculateDistanceMeters(points);

        Assert.InRange(distance, 60, 70);
    }

    [Theory]
    [InlineData(0, 5, 7, "05:07")]
    [InlineData(1, 2, 3, "01:02:03")]
    public void FormatDuration_ReturnsExpectedText(int hours, int minutes, int seconds, string expected)
    {
        var duration = new TimeSpan(hours, minutes, seconds);

        Assert.Equal(expected, RouteSessionMetrics.FormatDuration(duration));
    }

    [Theory]
    [InlineData(null, "Not available")]
    [InlineData(999.0, "999 m")]
    public void FormatDistance_ReturnsExpectedText(double? meters, string expected)
    {
        Assert.Equal(expected, RouteSessionMetrics.FormatDistance(meters));
    }

    [Fact]
    public void FormatDistance_WithKilometers_ReturnsOneDecimalKilometerText()
    {
        var expected = $"{1234.0 / 1000:0.0} km";

        Assert.Equal(expected, RouteSessionMetrics.FormatDistance(1234));
    }

    private static TrackPoint CreatePoint(double latitude, double longitude)
        => new(DateTimeOffset.UtcNow, latitude, longitude);
}
