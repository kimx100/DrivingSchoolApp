using System.Linq;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.Services.Tracking;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

public partial class LiveRoutePage : ContentPage
{
    private readonly TrackingCoordinator _tracking = TrackingCoordinator.Instance;

    private bool _mapInitialized;
    private bool _hasInitialViewport;
    private bool _handlersAttached;
    private bool _isTouchingMap;
    private bool _autoFollow = true;
    private bool _hasCenteredOnce;

    private CancellationTokenSource? _liveLocationCts;
    private PeriodicTimer? _elapsedTimer;

    private Location? _lastLiveLocation;
    private MPoint? _lastLiveWorldPoint;

    private const string BackgroundPromptKey = "BackgroundTrackingPromptShownV1";
    private const double FollowDisableBufferMeters = 120;
    private static readonly TimeSpan LiveLocationInterval = TimeSpan.FromSeconds(2);

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
        _ = PrimeInitialViewportAsync();
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
    
    private async Task PrimeInitialViewportAsync()
{
    if (_hasInitialViewport)
        return;

    try
    {
        var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (permission != PermissionStatus.Granted)
            return;

        var cached = await Geolocation.Default.GetLastKnownLocationAsync();
        if (cached is not null &&
            !(Math.Abs(cached.Latitude) < 0.0001 && Math.Abs(cached.Longitude) < 0.0001))
        {
            _lastLiveLocation = cached;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ZoomToRadius(cached.Latitude, cached.Longitude, 150);
                UpdateLiveLocation(cached);
                _hasInitialViewport = true;
            });
        }
    }
    catch
    {
        // ignore cache location failures
    }
}

private async Task TryPrimeFirstRoutePointAsync()
{
    try
    {
        var snapshot = _tracking.GetSnapshot();
        if (snapshot.HasFirstPoint)
            return;

        var location = await Geolocation.Default.GetLocationAsync(
            new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(4)));

        if (location is null)
            return;

        if (Math.Abs(location.Latitude) < 0.0001 && Math.Abs(location.Longitude) < 0.0001)
            return;

        var raw = new TrackPoint(
            Timestamp: DateTimeOffset.UtcNow,
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            AccuracyMeters: location.Accuracy,
            SpeedMps: location.Speed
        );

        if (_tracking.TryAcceptRawPoint(raw, out _, allowQuickFirstPoint: true))
            RefreshFromCoordinator();
    }
    catch
    {
        // ignore prime failures
    }
}

private void ZoomToRadius(double latitude, double longitude, double radiusMeters)
{
    if (RouteMapView.Map?.Navigator is null)
        return;

    var (x, y) = SphericalMercator.FromLonLat(longitude, latitude);
    var box = new MRect(
        x - radiusMeters,
        y - radiusMeters,
        x + radiusMeters,
        y + radiusMeters);

    _lastLiveWorldPoint = new MPoint(x, y);
    RouteMapView.Map.Navigator.ZoomToBox(box);
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

    private async void StartButton_Clicked(object? sender, EventArgs e)
    {
        var snapshotBefore = _tracking.GetSnapshot();

        if (snapshotBefore.IsTracking)
            return;

        var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (locationStatus != PermissionStatus.Granted)
        {
            await DisplayAlertAsync(AppText.LiveLocationRequiredTitle, AppText.LiveLocationRequiredMessage, AppText.CommonOk);
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
            if (!snapshotBefore.HasActiveSession)
                _tracking.ClearSession();
            else
                _tracking.PauseSession();

            await DisplayAlertAsync(AppText.LiveTrackingErrorTitle, AppText.LiveTrackingErrorMessage, AppText.CommonOk);
            return;
        }

        _autoFollow = true;
        RouteMapView.MyLocationFollow = true;
        RecenterButton.IsVisible = false;

        await TryPrimeFirstRoutePointAsync();
        RefreshFromCoordinator();
        await MaybeShowBackgroundTrackingPromptAsync();
    }

    private async void StopButton_Clicked(object? sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.IsTracking)
            return;

        await LessonTrackingPlatform.StopAsync();
        _tracking.PauseSession();

        RefreshFromCoordinator();
    }

    private async void EndButton_Clicked(object? sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.HasActiveSession)
            return;

        var confirmed = await DisplayAlertAsync(
            AppText.LiveEndRouteTitle,
            AppText.LiveEndRouteMessage,
            AppText.LiveEndRouteConfirm,
            AppText.CommonCancel);

        if (!confirmed)
            return;

        if (snapshot.IsTracking)
            await LessonTrackingPlatform.StopAsync();

        var savedSession = await _tracking.EndAndSaveAsync();

        _autoFollow = true;
        RecenterButton.IsVisible = false;
        RouteMapView.MyLocationFollow = false;
        _hasCenteredOnce = false;

        ClearRouteVisuals();
        RefreshFromCoordinator();

        if (savedSession is not null)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(RouteConfirmationPage)}?sessionId={Uri.EscapeDataString(savedSession.Id)}");
        }
    }

    private async void ResetButton_Clicked(object? sender, EventArgs e)
    {
        var snapshot = _tracking.GetSnapshot();
        if (!snapshot.HasActiveSession)
            return;

        var confirmed = await DisplayAlertAsync(
            AppText.LiveResetRouteTitle,
            AppText.LiveResetRouteMessage,
            AppText.LiveResetRouteConfirm,
            AppText.CommonCancel);

        if (!confirmed)
            return;

        if (snapshot.IsTracking)
            await LessonTrackingPlatform.StopAsync();

        _tracking.ClearSession();

        _autoFollow = true;
        RecenterButton.IsVisible = false;
        RouteMapView.MyLocationFollow = false;
        _hasCenteredOnce = false;

        ClearRouteVisuals();
        RefreshFromCoordinator();
    }

    private void RecenterButton_Clicked(object? sender, EventArgs e)
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
            var points = _tracking.GetAllPointsOldestFirst();
            RedrawRoute(points);

            if (_autoFollow)
                CenterOn(point.Latitude, point.Longitude);

            if (!_hasCenteredOnce)
                _hasCenteredOnce = true;

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
        HeroElapsedLabel.Text = FormatElapsed(snapshot.Elapsed);
        StartButton.IsEnabled = !snapshot.IsTracking;
        StartButton.Text = snapshot.IsPaused ? AppText.LiveResumeButton : AppText.LiveStartButton;
        StopButton.IsEnabled = snapshot.IsTracking;
        EndButton.IsEnabled = snapshot.HasActiveSession;
        ResetButton.IsEnabled = snapshot.HasActiveSession;

        FirstFixOverlay.IsVisible = snapshot.IsTracking && !snapshot.HasFirstPoint;

        if (snapshot.IsTracking)
        {
            if (_elapsedTimer is null)
                StartElapsedTimer();
        }
        else
        {
            StopElapsedTimer();
        }

        var points = _tracking.GetAllPointsOldestFirst();
        RedrawRoute(points);

        if (points.Count > 1 && !_hasCenteredOnce)
        {
            ZoomToPositions(points.Select(p => new Position(p.Latitude, p.Longitude)).ToList());
            _hasCenteredOnce = true;
        }
    }

    private void RedrawRoute(IReadOnlyList<TrackPoint> points)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        if (points.Count == 0)
            return;

        if (points.Count > 1)
        {
            var line = new Polyline
            {
                StrokeWidth = 6,
                StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#2F6FD6")
            };

            foreach (var point in points.OrderBy(p => p.Timestamp))
                line.Positions.Add(new Position(point.Latitude, point.Longitude));

            RouteMapView.Drawables.Add(line);
        }

        var first = points.First();

        RouteMapView.Pins.Add(new Pin
        {
            Label = AppText.RouteDetailStartPin,
            Position = new Position(first.Latitude, first.Longitude)
        });
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

    private void ZoomToPositions(IList<Position> positions)
    {
        if (RouteMapView.Map is null || positions.Count == 0)
            return;

        var world = positions
            .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
            .ToList();

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;

        foreach (var w in world)
        {
            var x = w.Item1;
            var y = w.Item2;

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        var box = new MRect(minX, minY, maxX, maxY);
        RouteMapView.Map.Navigator.ZoomToBox(box);
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
                return;

            try
            {
                var immediate = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(8)),
                    ct);

                if (immediate is not null &&
                    !(Math.Abs(immediate.Latitude) < 0.0001 && Math.Abs(immediate.Longitude) < 0.0001))
                {
                    UpdateLiveLocation(immediate);
                }
            }
            catch
            {
                // ignore immediate one-shot failure
            }

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
                    // keep polling
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

    private async Task MaybeShowBackgroundTrackingPromptAsync()
    {
#if ANDROID
        if (Preferences.Get(BackgroundPromptKey, false))
            return;

        Preferences.Set(BackgroundPromptKey, true);

        var choice = await DisplayActionSheetAsync(
            AppText.LiveBackgroundPrompt,
            AppText.LiveBackgroundPromptLater,
            null,
            AppText.LiveBackgroundPromptOpenSettings);

        if (choice == AppText.LiveBackgroundPromptOpenSettings)
        {
            try
            {
                AppInfo.Current.ShowSettingsUI();
            }
            catch
            {
                // ignore
            }
        }
#endif
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return elapsed.ToString(@"hh\:mm\:ss");

        return elapsed.ToString(@"mm\:ss");
    }
}
