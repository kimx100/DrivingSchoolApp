using System.Diagnostics;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;

namespace DrivingSchoolApp.Services;

public static class RouteMapViewportHelper
{
    public const float RouteEndpointPinScale = 0.55f;

    private const double RouteFitPaddingRatio = 0.25;
    private const double RouteFitBottomPaddingRatio = 0.18;
    private const double MinimumRouteFitPaddingMeters = 220;
    private const double MinimumRouteFitBottomPaddingMeters = 180;
    private const double RouteFitResolutionZoomOutFactor = 1.15;

    public static void FitRoute(
        MapView mapView,
        IReadOnlyList<Position> positions,
        bool accountForBottomOverlay = false,
        string debugSource = "")
    {
        if (positions.Count == 0)
            return;

        var routePositions = positions.ToList();

        void ApplyWhenSized()
        {
            if (mapView.Width <= 0 || mapView.Height <= 0)
                return;

            ApplyRouteFit(mapView, routePositions, accountForBottomOverlay, debugSource);
        }

        if (mapView.Width <= 0 || mapView.Height <= 0)
        {
            EventHandler? sizeChangedHandler = null;
            sizeChangedHandler = (_, _) =>
            {
                if (mapView.Width <= 0 || mapView.Height <= 0)
                    return;

                mapView.SizeChanged -= sizeChangedHandler;
                ApplyRouteFit(mapView, routePositions, accountForBottomOverlay, debugSource);
            };

            mapView.SizeChanged += sizeChangedHandler;
            return;
        }

        mapView.Dispatcher.Dispatch(ApplyWhenSized);
    }

    private static void ApplyRouteFit(
        MapView mapView,
        IReadOnlyList<Position> positions,
        bool accountForBottomOverlay,
        string debugSource)
    {
        var navigator = mapView.Map?.Navigator;

        if (navigator is null || positions.Count == 0)
            return;

        var world = positions
            .Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude))
            .ToList();

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;

        foreach (var point in world)
        {
            if (point.Item1 < minX) minX = point.Item1;
            if (point.Item2 < minY) minY = point.Item2;
            if (point.Item1 > maxX) maxX = point.Item1;
            if (point.Item2 > maxY) maxY = point.Item2;
        }

        var width = Math.Max(maxX - minX, 1);
        var height = Math.Max(maxY - minY, 1);
        var paddingX = Math.Max(width * RouteFitPaddingRatio, MinimumRouteFitPaddingMeters);
        var paddingY = Math.Max(height * RouteFitPaddingRatio, MinimumRouteFitPaddingMeters);
        var bottomPadding = accountForBottomOverlay
            ? Math.Max(height * RouteFitBottomPaddingRatio, MinimumRouteFitBottomPaddingMeters)
            : 0;

        var paddedBox = new MRect(
            minX - paddingX,
            minY - paddingY - bottomPadding,
            maxX + paddingX,
            maxY + paddingY);

        // Mapsui's box fit can still feel tight on small route preview maps; a mild resolution bump adds breathing room.
        navigator.ZoomToBox(paddedBox, MBoxFit.Fit, 0);
        var fittedResolution = navigator.Viewport.Resolution;

        if (!double.IsNaN(fittedResolution) && !double.IsInfinity(fittedResolution) && fittedResolution > 0)
            navigator.ZoomTo(fittedResolution * RouteFitResolutionZoomOutFactor, 0);

#if DEBUG
        Debug.WriteLine(
            $"Route map fit: {debugSource}, positions={positions.Count}, bottomOverlay={accountForBottomOverlay}, resolution={navigator.Viewport.Resolution:0.##}");
#endif
    }
}
