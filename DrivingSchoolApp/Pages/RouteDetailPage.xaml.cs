using System.Linq;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Pages;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class RouteDetailPage : ContentPage
{
    private readonly OsrmRoadSnapService _snapService = new();

    private bool _mapInitialized;
    private bool _loaded;
    private bool _isSnapping;

    private string? _sessionId;
    private RouteSession? _session;
    private SnappedRouteCache? _snapCache;
    private RouteSnapState? _snapState;

    public string? SessionId
    {
        get => _sessionId;
        set
        {
            _sessionId = Uri.UnescapeDataString(value ?? string.Empty);
            _loaded = false;
        }
    }

    public RouteDetailPage()
    {
        InitializeComponent();
        EnsureMapInitialized();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded || string.IsNullOrWhiteSpace(SessionId))
            return;

        await LoadRouteAsync();
    }

    private void EnsureMapInitialized()
    {
        if (_mapInitialized)
            return;

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer("DrivingSchoolApp"));

        RouteMapView.Map = map;
        RouteMapView.MyLocationEnabled = false;
        RouteMapView.MyLocationFollow = false;

        _mapInitialized = true;
    }

    private async Task LoadRouteAsync()
    {
        LoadingMessageLabel.Text = "Loading route...";
        LoadingOverlay.IsVisible = true;

        _session = await RouteStorage.LoadAsync(SessionId!);

        if (_session is null || _session.Points is not { Count: > 0 })
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Route not found", "This saved route could not be loaded.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _snapCache = await RouteSnapStorage.LoadAsync(_session.Id);
        _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);

        if (_snapState?.Status is RouteSnapWorkStatus.Pending or RouteSnapWorkStatus.Processing)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert(
                "Still processing",
                "This route is still being processed. Please wait until it is ready to view.",
                "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        Title = _session.StartedAt.LocalDateTime.ToString("dd MMM yyyy");
        DrawBestAvailableRoute();
        UpdateSnapToolbarText();

        if (_snapState?.Status == RouteSnapWorkStatus.ReadyToView)
        {
            await RouteSnapBackgroundProcessor.MarkViewedAsync(_session.Id);
            _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);
            UpdateSnapToolbarText();
        }

        _loaded = true;
        LoadingOverlay.IsVisible = false;
    }

    private void DrawBestAvailableRoute()
    {
        if (_session is null)
            return;

        if (HasUsableSnappedGeometry())
        {
            DrawCoordinates(_snapCache!.Geometry);
            return;
        }

        DrawTrackPoints(_session.Points);
    }

    private bool HasUsableSnappedGeometry()
    {
        if (_session is null || _snapCache is null)
            return false;

        if (_snapCache.SessionId != _session.Id)
            return false;

        if (_snapCache.SourcePointCount != _session.Points.Count)
            return false;

        return _snapCache.Geometry is { Count: > 1 };
    }

    private void UpdateSnapToolbarText()
    {
        if (_snapState?.Status == RouteSnapWorkStatus.Failed)
        {
            SnapToolbarItem.Text = "Retry snap";
        }
        else if (HasUsableSnappedGeometry())
        {
            SnapToolbarItem.Text = "Re-snap";
        }
        else
        {
            SnapToolbarItem.Text = "Snap";
        }

        SnapToolbarItem.IsEnabled = !_isSnapping;
    }

    private void DrawTrackPoints(IReadOnlyList<TrackPoint> points)
    {
        var ordered = points
            .OrderBy(p => p.Timestamp)
            .Select(p => new Position(p.Latitude, p.Longitude))
            .ToList();

        DrawPositions(ordered);
    }

    private void DrawCoordinates(IReadOnlyList<RouteCoordinate> geometry)
    {
        var positions = geometry
            .Select(p => new Position(p.Latitude, p.Longitude))
            .ToList();

        DrawPositions(positions);
    }

    private void DrawPositions(IReadOnlyList<Position> positions)
    {
        RouteMapView.Drawables.Clear();
        RouteMapView.Pins.Clear();

        if (positions.Count == 0)
            return;

        if (positions.Count > 1)
        {
            var line = new Polyline
            {
                StrokeWidth = 7,
                StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#2F6FD6")
            };

            foreach (var position in positions)
                line.Positions.Add(position);

            RouteMapView.Drawables.Add(line);
        }

        var first = positions.First();
        var last = positions.Last();

        RouteMapView.Pins.Add(new Pin
        {
            Label = "Start",
            Position = first
        });

        RouteMapView.Pins.Add(new Pin
        {
            Label = "End",
            Position = last
        });

        ZoomToPositions(positions);
    }

    private void ZoomToPositions(IReadOnlyList<Position> positions)
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

    private async void SnapToolbarItem_Clicked(object? sender, EventArgs e)
    {
        if (_session is null || _isSnapping)
            return;

        _isSnapping = true;
        UpdateSnapToolbarText();

        LoadingMessageLabel.Text = HasUsableSnappedGeometry()
            ? "Re-snapping route..."
            : "Snapping route...";
        LoadingOverlay.IsVisible = true;

        try
        {
            var result = await _snapService.TrySnapAsync(_session);

            if (!result.Success || result.Cache is null)
            {
                await DisplayAlert("Snap unavailable", result.Message, "OK");
                return;
            }

            _snapCache = result.Cache;
            await RouteSnapStorage.SaveAsync(result.Cache);
            await RouteSnapBackgroundProcessor.MarkManualSnapSucceededAsync(_session.Id);

            _snapState = await RouteSnapBackgroundProcessor.GetStateAsync(_session.Id);

            DrawBestAvailableRoute();
            UpdateSnapToolbarText();

            await DisplayAlert(
                "Route snapped",
                "The saved route has been polished and cached for offline viewing.",
                "OK");
        }
        finally
        {
            _isSnapping = false;
            LoadingOverlay.IsVisible = false;
            UpdateSnapToolbarText();
        }
    }

    private async void DeleteToolbarItem_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return;

        var confirmed = await DisplayAlert(
            "Delete route?",
            "This saved route will be deleted.",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        await RouteStorage.DeleteAsync(SessionId);

        if (Preferences.Get("LastRouteSessionId", string.Empty) == SessionId)
            Preferences.Set("LastRouteSessionId", string.Empty);

        await Shell.Current.GoToAsync("..");
    }
}