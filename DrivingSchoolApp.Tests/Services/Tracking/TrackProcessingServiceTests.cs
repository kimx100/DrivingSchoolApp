using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services.Tracking;

namespace DrivingSchoolApp.Tests.Services.Tracking;

public class TrackProcessingServiceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryProcess_FirstValidPoint_ReturnsTrue()
    {
        var service = new TrackProcessingService();
        var raw = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 5);

        var accepted = service.TryProcess(raw, out var processed);

        Assert.True(accepted);
        Assert.Equal(raw.Latitude, processed.Latitude);
        Assert.Equal(raw.Longitude, processed.Longitude);
    }

    [Fact]
    public void TryProcess_FirstPointWithPoorAccuracy_ReturnsFalse()
    {
        var service = new TrackProcessingService();
        var raw = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 100);

        var accepted = service.TryProcess(raw, out _);

        Assert.False(accepted);
    }

    [Fact]
    public void TryProcess_FirstPointWithQuickFirstAllowance_AcceptsAccuracyUnder150()
    {
        var service = new TrackProcessingService();
        var raw = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 120);

        var accepted = service.TryProcess(raw, out var processed, allowQuickFirstPoint: true);

        Assert.True(accepted);
        Assert.Equal(raw.Latitude, processed.Latitude);
        Assert.Equal(raw.Longitude, processed.Longitude);
    }

    [Fact]
    public void TryProcess_TinyMovementAfterAcceptedPoint_ReturnsFalse()
    {
        var service = new TrackProcessingService();
        var first = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 5);
        var tinyMove = CreatePoint(BaseTime.AddSeconds(5), 55.6761, 12.56831, accuracyMeters: 5);

        Assert.True(service.TryProcess(first, out _));

        var accepted = service.TryProcess(tinyMove, out _);

        Assert.False(accepted);
    }

    [Fact]
    public void TryProcess_ReasonableMovementAfterAcceptedPoint_ReturnsTrue()
    {
        var service = new TrackProcessingService();
        var first = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 5);
        var moved = CreatePoint(BaseTime.AddSeconds(5), 55.6761, 12.5684, accuracyMeters: 5);

        Assert.True(service.TryProcess(first, out var firstProcessed));

        var accepted = service.TryProcess(moved, out var processed);

        Assert.True(accepted);
        Assert.Equal(firstProcessed.Latitude, processed.Latitude);
        Assert.InRange(processed.Longitude, firstProcessed.Longitude, moved.Longitude);
        Assert.True(processed.Longitude > firstProcessed.Longitude);
    }

    [Fact]
    public void Reset_AfterRejectedOrAcceptedPoint_AllowsNewFirstPoint()
    {
        var service = new TrackProcessingService();
        var first = CreatePoint(BaseTime, 55.6761, 12.5683, accuracyMeters: 5);
        var tinyMove = CreatePoint(BaseTime.AddSeconds(5), 55.6761, 12.56831, accuracyMeters: 5);
        var afterReset = CreatePoint(BaseTime.AddSeconds(10), 55.68, 12.57, accuracyMeters: 5);

        Assert.True(service.TryProcess(first, out _));
        Assert.False(service.TryProcess(tinyMove, out _));

        service.Reset();

        var accepted = service.TryProcess(afterReset, out var processed);

        Assert.True(accepted);
        Assert.Equal(afterReset.Latitude, processed.Latitude);
        Assert.Equal(afterReset.Longitude, processed.Longitude);
    }

    private static TrackPoint CreatePoint(
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double accuracyMeters)
        => new(
            Timestamp: timestamp,
            Latitude: latitude,
            Longitude: longitude,
            AccuracyMeters: accuracyMeters);
}
