using System.Collections.ObjectModel;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Pages;

public partial class TrackingPage : ContentPage
{
    private readonly ObservableCollection<TrackPoint> _points = new();
    private CancellationTokenSource? _cts;

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
                        _points.Insert(0, p);
                        if (_points.Count > 200) _points.RemoveAt(_points.Count - 1);
                        UpdateCount();
                        StatusLabel.Text = $"Status: tracking… ({p.Latitude:F6}, {p.Longitude:F6})";
                    });
                }
                catch
                {
                    // keep running even if a tick fails
                }
            }
        }, ct);
    }

    private void Stop_Clicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        StatusLabel.Text = "Status: stopped";
    }

    private void Clear_Clicked(object sender, EventArgs e)
    {
        _points.Clear();
        UpdateCount();
    }

    private void UpdateCount()
        => CountLabel.Text = $"Points: {_points.Count}";
}