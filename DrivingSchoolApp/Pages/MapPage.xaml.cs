using System.Linq;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace DrivingSchoolApp.Pages;

public partial class MapPage
{
    private bool _mapInitialized;

    public MapPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAndDrawAsync();
    }

    private async Task LoadAndDrawAsync()
    {
        EnsureMapInitialized();

        // Give the control a moment to size itself before navigation/zoom
        await Task.Delay(200);

        var lastId = Preferences.Get("LastRouteSessionId", string.Empty);

        if (!string.IsNullOrWhiteSpace(lastId))
        {
            var session = await RouteStorage.LoadAsync(lastId);
            if (session?.Points is { Count: > 1 })
            {
                DrawRoute(session.Points);
                return;
            }
        }

        DrawTestRoute();
        
    }

    private void EnsureMapInitialized()
    {
        if (_mapInitialized) return;

        var map = new Mapsui.Map();

        // IMPORTANT: Provide your own user-agent string to avoid OSM blocking generic clients
        // (OSM has blocked some default tile user agents in the past) :contentReference[oaicite:6]{index=6}
        map.Layers.Add(OpenStreetMap.CreateTileLayer("DrivingSchoolApp"));

        RouteMapView.Map = map;

        _mapInitialized = true;
    }

    private void SetMyLocation(Position pos)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update the internal MyLocationLayer so the MyLocation button
            // centers to the correct position (instead of 0,0)
            RouteMapView.MyLocationLayer.UpdateMyLocation(pos);
            RouteMapView.MyLocationEnabled = true;
            RouteMapView.MyLocationFollow = false;
        });
    }
    
    private void DrawTestRoute()
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        var start = new Position(55.6761, 12.5683);
        var end = new Position(55.6830, 12.5710);

        var line = new Polyline
        {
            StrokeWidth = 6,
            StrokeColor = Microsoft.Maui.Graphics.Colors.Blue
        };

        line.Positions.Add(start);
        line.Positions.Add(end);

        RouteMapView.Drawables.Add(line);

        ZoomToPositions(new[] { start, end });
        SetMyLocation(end);
    }

    private void DrawRoute(List<TrackPoint> points)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        // Your TrackingPage inserts newest first -> sort to draw in order
        var ordered = points.OrderBy(p => p.Timestamp).ToList();

        var line = new Polyline
        {
            StrokeWidth = 6,
            StrokeColor = Microsoft.Maui.Graphics.Colors.Blue
        };

        foreach (var p in ordered)
            line.Positions.Add(new Position(p.Latitude, p.Longitude));

        RouteMapView.Drawables.Add(line);

// Center to the route bounds
        ZoomToPositions(line.Positions);
        SetMyLocation(line.Positions[^1]);

// Optional: also center to the latest point (helps you verify it's correct)
        var last = ordered[^1];
        var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(last.Longitude, last.Latitude);
        RouteMapView.Map?.Navigator?.CenterOn(new Mapsui.MPoint(x, y));
    }

    private void ZoomToPositions(IList<Position> positions)
    {
        if (RouteMapView.Map is null) return;
        if (positions.Count == 0) return;

        // FromLonLat(double lon, double lat) returns (double x, double y)
        var world = positions
            .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
            .ToList();

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;

        foreach (var w in world)
        {
            // tuple fields are always accessible via Item1/Item2
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