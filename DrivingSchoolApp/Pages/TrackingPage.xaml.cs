using System.Collections.ObjectModel;
using System.Linq;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

public partial class TrackingPage : ContentPage
{
    private readonly ObservableCollection<TrackPoint> _points = new();
    private CancellationTokenSource? _cts;

// Filtering/smoothing state
    private TrackPoint? _lastAccepted;
    private double _emaLat;
    private double _emaLon;
    private bool _emaInit;

    public TrackingPage()
    {
        InitializeComponent();
        PointsList.ItemsSource = _points;
        UpdateCount();
    }

    private async void Start_Clicked(object sender, EventArgs e)
    {
        if (_cts is not null) return;

        StatusLabel.Text = "Status: requesting permission…";
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Status: permission denied";
            return;
        }

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        StatusLabel.Text = "Status: tracking…";

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                    var location = await Geolocation.Default.GetLocationAsync(request, ct);

                    if (location is null) continue;

                    var p = new TrackPoint(
                        Timestamp: DateTimeOffset.UtcNow,
                        Latitude: location.Latitude,
                        Longitude: location.Longitude,
                        AccuracyMeters: location.Accuracy,
                        SpeedMps: location.Speed
                    );

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!TryProcessPoint(p, out var processed))
                            return;

                        _points.Insert(0, processed);
                        if (_points.Count > 200) _points.RemoveAt(_points.Count - 1);
                        UpdateCount();
                        StatusLabel.Text = $"Status: tracking… ({processed.Latitude:F6}, {processed.Longitude:F6})";
                    });
                }
                catch
                {
                    // keep running even if a tick fails
                }
            }
        }, ct);
    }

    private async void Stop_Clicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // Save route (if we have enough points)
        if (_points.Count > 1)
        {
            var ordered = _points.OrderBy(p => p.Timestamp).ToList();

            var session = new RouteSession
            {
                StartedAt = ordered.First().Timestamp,
                EndedAt = ordered.Last().Timestamp,
                Points = ordered
            };

            await RouteStorage.SaveAsync(session);
            Preferences.Set("LastRouteSessionId", session.Id);

            StatusLabel.Text = $"Status: stopped (saved {ordered.Count} points)";
        }
        else
        {
            StatusLabel.Text = "Status: stopped";
        }
    }

    private void Clear_Clicked(object sender, EventArgs e)
    {
        _points.Clear();
        UpdateCount();
    }

    private void UpdateCount()
        => CountLabel.Text = $"Points: {_points.Count}";
    
    private bool TryProcessPoint(TrackPoint raw, out TrackPoint processed)
{
    processed = raw;

    var acc = raw.AccuracyMeters ?? 9999;

    // 1) Drop very inaccurate points (tweak later)
    if (acc > 40) // meters
        return false;

    if (_lastAccepted is not null)
    {
        var prev = _lastAccepted;

        var dt = (raw.Timestamp - prev.Timestamp).TotalSeconds;
        if (dt <= 0.5)
            return false;

        var dist = HaversineMeters(prev.Latitude, prev.Longitude,
            raw.Latitude, raw.Longitude);

        var impliedSpeed = dist / dt; // m/s

        // 2) Drop impossible jumps (GPS glitches)
        if (dist > 250 && dt < 2.5) // jumped 250m too fast
            return false;

        if (impliedSpeed > 70) // > 252 km/h (glitch)
            return false;

        // 3) Ignore tiny jitter
        if (dist < 2)
            return false;
    }

    // 4) Smooth (EMA) — less smoothing at higher speeds
    var speed = raw.SpeedMps ?? 0;
    var alpha = 0.12 + Clamp(speed / 30.0, 0, 1) * 0.38; // 0.12 .. 0.50

    // If accuracy is mediocre, smooth more
    if (acc > 20) alpha *= 0.7;

    if (!_emaInit)
    {
        _emaLat = raw.Latitude;
        _emaLon = raw.Longitude;
        _emaInit = true;
    }
    else
    {
        _emaLat = _emaLat + alpha * (raw.Latitude - _emaLat);
        _emaLon = _emaLon + alpha * (raw.Longitude - _emaLon);
    }

    processed = raw with { Latitude = _emaLat, Longitude = _emaLon };

    _lastAccepted = processed;
    return true;
}

private static double Clamp(double v, double min, double max)
    => v < min ? min : (v > max ? max : v);

private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
{
    const double R = 6371000.0;
    static double ToRad(double deg) => deg * (Math.PI / 180.0);

    var dLat = ToRad(lat2 - lat1);
    var dLon = ToRad(lon2 - lon1);

    var a =
        Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
        Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    return R * c;
}
}