using System.Linq;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services.Tracking;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace DrivingSchoolApp.Pages;

public partial class LiveRoutePage : ContentPage
{
    private readonly TrackingCoordinator _tracking = TrackingCoordinator.Instance;

    private bool _mapInitialized;
    private bool _handlersAttached;
    private bool _isTouchingMap;
    private bool _autoFollow = true;
    private bool _hasCenteredOnTrackedPoint;

    private CancellationTokenSource? _liveLocationCts;
    private PeriodicTimer? _elapsedTimer;

    private Location? _lastLiveLocation;
    private MPoint? _lastLiveWorldPoint;

    private const double FollowDisableBufferMeters = 120;
    private static readonly TimeSpan LiveLocationInterval = TimeSpan.FromSeconds(3);

    public LiveRoutePage()
    {
        InitializeComponent();
        EnsureMapInitialized();
        RefreshFromCoordinator();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _tracking.PointAccepted += OnPointAccepted;
        _tracking.SnapshotChanged += OnSnapshotChanged;

        RefreshFromCoordinator();
        StartLiveLocationLoop();
    }

    protected override void OnDisappearing()
    {
        _tracking.PointAccepted -= OnPointAccepted;
        _tracking.SnapshotChanged -= OnSnapshotChanged;

        StopLiveLocationLoop();
        StopElapsedTimer();

        base.OnDisappearing();
    }

    private void EnsureMapInitialized()
    {
        if (_mapInitialized)
            return;

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer("DrivingSchoolApp"));

        RouteMapView.Map = map;
        RouteMapView.MyLocationEnabled = true;
        RouteMapView.MyLocationFollow = false;

        AttachMapHandlers();
        _mapInitialized = true;
    }

    private void AttachMapHandlers()
    {
        if (_handlersAttached)
            return;

        RouteMapView.MapPointerPressed += RouteMapView_MapPointerPressed;
        RouteMapView.MapPointerMoved += RouteMapView_MapPointerMoved;
        RouteMapView.MapPointerReleased += RouteMapView_MapPointerReleased;

        _handlersAttached = true;
    }

    private void RouteMapView_MapPointerPressed(object? sender, MapEventArgs e)
    {
        _isTouchingMap = true;
    }

    private void RouteMapView_MapPointerMoved(object? sender, MapEventArgs e)
    {
        if (!_isTouchingMap)
            return;

        DisableFollowIfDraggedAway();
    }

    private void RouteMapView_MapPointerReleased(object? sender, MapEventArgs e)
    {
        _isTouchingMap = false;
    }

    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        var before = _tracking.GetSnapshot();

        if (before.IsTracking)
            return;

        var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (locationStatus != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Location permission denied";
            return;
        }

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _ = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
#endif

        if (!_tracking.StartOrResumeSession())
        {
            RefreshFromCoordinator();
            return;
        }

        var serviceStarted = await LessonTrackingPlatform.StartAsync();
        if (!serviceStarted)
        {
            if (!before.HasActiveSession)
                _tracking.ClearSession();
            else
                _tracking.PauseSession();

            StatusLabel.Text = "Could not start background tracking";
            return;
        }

        _autoFollow = true;
        RecenterButton.IsVisible = false;
        RouteMapView.MyLocationFollow = true;

        RefreshFromCoordinator();
    }

    private async void StopButton_Clicked(object sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.IsTracking)
            return;

        await LessonTrackingPlatform.StopAsync();
        _tracking.PauseSession();

        RefreshFromCoordinator();
    }

    private async void EndButton_Clicked(object sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.HasActiveSession)
            return;

        var confirmed = await DisplayAlert(
            "End route?",
            "This will finalize and save the current route.",
            "End route",
            "Cancel");

        if (!confirmed)
            return;

        if (snapshot.IsTracking)
            await LessonTrackingPlatform.StopAsync();

        var savedSession = await _tracking.EndAndSaveAsync();

        _autoFollow = true;
        RecenterButton.IsVisible = false;
        RouteMapView.MyLocationFollow = false;
        _hasCenteredOnTrackedPoint = false;

        ClearRouteVisuals();
        RefreshFromCoordinator();

        if (savedSession is not null)
            StatusLabel.Text = $"Route saved ({savedSession.Points.Count} points)";
        else
            StatusLabel.Text = "Route ended";
    }

    private async void ResetButton_Clicked(object sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.HasActiveSession)
            return;

        var confirmed = await DisplayAlert(
            "Reset route?",
            "This will discard the current route and clear all collected points.",
            "Reset route",
            "Cancel");

        if (!confirmed)
            return;

        if (snapshot.IsTracking)
            await LessonTrackingPlatform.StopAsync();

        _tracking.ClearSession();

        _autoFollow = true;
        RecenterButton.IsVisible = false;
        RouteMapView.MyLocationFollow = false;
        _hasCenteredOnTrackedPoint = false;

        ClearRouteVisuals();
        RefreshFromCoordinator();
    }

    private void RecenterButton_Clicked(object sender, EventArgs e)
    {
        _autoFollow = true;
        RouteMapView.MyLocationFollow = true;
        RecenterButton.IsVisible = false;

        var snapshot = _tracking.GetSnapshot();

        if (snapshot.LatestPoint is not null)
        {
            CenterOn(snapshot.LatestPoint.Latitude, snapshot.LatestPoint.Longitude);
            return;
        }

        if (_lastLiveLocation is not null)
            CenterOn(_lastLiveLocation.Latitude, _lastLiveLocation.Longitude);
    }

    private void OnPointAccepted(TrackPoint point)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RedrawRoute(_tracking.GetAllPointsOldestFirst());

            if (_autoFollow)
                CenterOn(point.Latitude, point.Longitude);

            if (!_hasCenteredOnTrackedPoint)
                _hasCenteredOnTrackedPoint = true;

            RefreshFromCoordinator();
        });
    }

    private void OnSnapshotChanged(TrackingSnapshot snapshot)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshFromSnapshot(snapshot);
        });
    }

    private void RefreshFromCoordinator()
    {
        RefreshFromSnapshot(_tracking.GetSnapshot());
    }

    private void RefreshFromSnapshot(TrackingSnapshot snapshot)
    {
        UpdateSummary(snapshot);
        UpdateButtons(snapshot);
        UpdateLoadingOverlay(snapshot);
        UpdateLocationText(snapshot.LatestPoint);

        if (snapshot.IsTracking)
        {
            if (_elapsedTimer is null)
                StartElapsedTimer();
        }
        else
        {
            StopElapsedTimer();
        }

        RedrawRoute(_tracking.GetAllPointsOldestFirst());
    }

    private void UpdateSummary(TrackingSnapshot snapshot)
    {
        ElapsedLabel.Text = FormatElapsed(snapshot.Elapsed);
        HeroElapsedLabel.Text = FormatElapsed(snapshot.Elapsed);
        CountLabel.Text = $"Points: {snapshot.TotalPointCount}";

        if (snapshot.IsTracking && !snapshot.HasFirstPoint)
            StatusLabel.Text = "Waiting for GPS...";
        else if (snapshot.IsTracking)
            StatusLabel.Text = "Tracking live";
        else if (snapshot.IsPaused)
            StatusLabel.Text = "Tracking paused";
        else
            StatusLabel.Text = "Ready to start";
    }

    private void UpdateButtons(TrackingSnapshot snapshot)
    {
        StartButton.IsEnabled = !snapshot.IsTracking;
        StartButton.Text = snapshot.IsPaused ? "Resume" : "Start";

        StopButton.IsEnabled = snapshot.IsTracking;
        EndButton.IsEnabled = snapshot.HasActiveSession;
        ResetButton.IsEnabled = snapshot.HasActiveSession;
    }

    private void UpdateLoadingOverlay(TrackingSnapshot snapshot)
    {
        FirstFixOverlay.IsVisible = snapshot.IsTracking && !snapshot.HasFirstPoint;
    }

    private void UpdateLocationText(TrackPoint? trackedPoint)
    {
        if (trackedPoint is not null)
        {
            LocationLabel.Text = $"Lat {trackedPoint.Latitude:F5} · Lng {trackedPoint.Longitude:F5}";
            return;
        }

        if (_lastLiveLocation is not null)
        {
            LocationLabel.Text = $"Lat {_lastLiveLocation.Latitude:F5} · Lng {_lastLiveLocation.Longitude:F5}";
            return;
        }

        LocationLabel.Text = "Location unavailable";
    }

    private void RedrawRoute(IReadOnlyList<TrackPoint> points)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        if (points.Count <= 1)
            return;

        var line = new Polyline
        {
            StrokeWidth = 6,
            StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#2F6FD6")
        };

        foreach (var point in points.OrderBy(p => p.Timestamp))
            line.Positions.Add(new Position(point.Latitude, point.Longitude));

        RouteMapView.Drawables.Add(line);
    }

    private void ClearRouteVisuals()
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();
    }

    private void CenterOn(double latitude, double longitude)
    {
        if (RouteMapView.Map?.Navigator is null)
            return;

        var (x, y) = SphericalMercator.FromLonLat(longitude, latitude);
        var worldPoint = new MPoint(x, y);

        _lastLiveWorldPoint = worldPoint;
        RouteMapView.Map.Navigator.CenterOn(worldPoint);
    }

    private void DisableFollowIfDraggedAway()
    {
        if (!_autoFollow || _lastLiveWorldPoint is null)
            return;

        var navigator = RouteMapView.Map?.Navigator;
        if (navigator is null)
            return;

        var viewport = navigator.Viewport;
        var center = new MPoint(viewport.CenterX, viewport.CenterY);
        var distance = center.Distance(_lastLiveWorldPoint);

        if (distance <= FollowDisableBufferMeters)
            return;

        _autoFollow = false;
        RouteMapView.MyLocationFollow = false;
        RecenterButton.IsVisible = true;
    }

    private void StartLiveLocationLoop()
    {
        if (_liveLocationCts is not null)
            return;

        _liveLocationCts = new CancellationTokenSource();
        _ = RunLiveLocationLoopAsync(_liveLocationCts.Token);
    }

    private async Task RunLiveLocationLoopAsync(CancellationToken ct)
    {
        try
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (permission != PermissionStatus.Granted)
                return;

            using var timer = new PeriodicTimer(LiveLocationInterval);

            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(5));

                    var location = await Geolocation.Default.GetLocationAsync(request, ct);
                    if (location is null)
                        continue;

                    if (Math.Abs(location.Latitude) < 0.0001 && Math.Abs(location.Longitude) < 0.0001)
                        continue;

                    UpdateLiveLocation(location);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // ignore failed ticks and keep polling
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private void UpdateLiveLocation(Location location)
    {
        _lastLiveLocation = location;

        var livePosition = new Position(location.Latitude, location.Longitude);
        var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        _lastLiveWorldPoint = new MPoint(x, y);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RouteMapView.MyLocationEnabled = true;
            RouteMapView.MyLocationLayer.UpdateMyLocation(livePosition, false);

            if (location.Speed is double speed && speed >= 0)
                RouteMapView.MyLocationLayer.UpdateMySpeed(speed);

            UpdateLocationText(_tracking.GetSnapshot().LatestPoint);

            if (_autoFollow)
                RouteMapView.Map?.Navigator?.CenterOn(_lastLiveWorldPoint);
        });
    }

    private void StopLiveLocationLoop()
    {
        var cts = _liveLocationCts;
        _liveLocationCts = null;

        if (cts is null)
            return;

        cts.Cancel();
        cts.Dispose();

        _isTouchingMap = false;
    }

    private void StartElapsedTimer()
    {
        _elapsedTimer?.Dispose();
        _elapsedTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        _ = Task.Run(async () =>
        {
            var timer = _elapsedTimer;
            if (timer is null)
                return;

            try
            {
                while (timer == _elapsedTimer && await timer.WaitForNextTickAsync())
                {
                    var snapshot = _tracking.GetSnapshot();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ElapsedLabel.Text = FormatElapsed(snapshot.Elapsed);
                        HeroElapsedLabel.Text = FormatElapsed(snapshot.Elapsed);
                    });
                }
            }
            catch
            {
                // ignore timer disposal/cancellation issues
            }
        });
    }

    private void StopElapsedTimer()
    {
        _elapsedTimer?.Dispose();
        _elapsedTimer = null;
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return elapsed.ToString(@"hh\:mm\:ss");

        return elapsed.ToString(@"mm\:ss");
    }
}