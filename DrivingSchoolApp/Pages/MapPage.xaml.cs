using System.Linq;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

public partial class MapPage : ContentPage
{
    private bool _mapInitialized;
    private bool _handlersAttached;
    private bool _isTouchingMap;

    private CancellationTokenSource? _liveLocationCts;
    private MPoint? _lastLiveWorldPoint;

    private const double FollowDisableBufferMeters = 120;
    private static readonly TimeSpan LiveLocationInterval = TimeSpan.FromSeconds(3);

    public MapPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAndDrawAsync();
        StartLiveLocationLoop();
    }

    protected override void OnDisappearing()
    {
        StopLiveLocationLoop();
        base.OnDisappearing();
    }

    private async Task LoadAndDrawAsync()
    {
        EnsureMapInitialized();

        await Task.Delay(200);

        var lastId = Preferences.Get("LastRouteSessionId", string.Empty);
        if (string.IsNullOrWhiteSpace(lastId))
            return;

        var session = await RouteStorage.LoadAsync(lastId);
        if (session?.Points is { Count: > 1 })
        {
            DrawRoute(session.Points);
        }
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

    private void UpdateLiveLocation(Location location)
    {
        var livePosition = new Position(location.Latitude, location.Longitude);
        var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        _lastLiveWorldPoint = new MPoint(x, y);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RouteMapView.MyLocationEnabled = true;
            RouteMapView.MyLocationLayer.UpdateMyLocation(livePosition, false);

            if (location.Speed is double speed && speed >= 0)
                RouteMapView.MyLocationLayer.UpdateMySpeed(speed);

            if (RouteMapView.MyLocationFollow && _lastLiveWorldPoint is not null)
            {
                RouteMapView.Map?.Navigator?.CenterOn(_lastLiveWorldPoint);
            }
        });
    }

    private void DisableFollowIfDraggedAway()
    {
        if (!RouteMapView.MyLocationFollow || _lastLiveWorldPoint is null)
            return;

        var navigator = RouteMapView.Map?.Navigator;
        if (navigator is null)
            return;

        var viewport = navigator.Viewport;
        var center = new MPoint(viewport.CenterX, viewport.CenterY);
        var distance = center.Distance(_lastLiveWorldPoint);

        if (distance > FollowDisableBufferMeters)
            RouteMapView.MyLocationFollow = false;
    }

    private void DrawRoute(List<TrackPoint> points)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        var ordered = points.OrderBy(p => p.Timestamp).ToList();

        var line = new Polyline
        {
            StrokeWidth = 6,
            StrokeColor = Microsoft.Maui.Graphics.Colors.Blue
        };

        foreach (var p in ordered)
            line.Positions.Add(new Position(p.Latitude, p.Longitude));

        RouteMapView.Drawables.Add(line);
        ZoomToPositions(line.Positions);
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
}