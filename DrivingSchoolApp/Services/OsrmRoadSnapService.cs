using System.Globalization;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using DrivingSchoolApp.Models;
using Microsoft.Maui.Networking;

namespace DrivingSchoolApp.Services;

public sealed class OsrmRoadSnapService
{
    private const int ResponseLogPrefixLength = 500;

    private static readonly HttpClient Http = CreateHttpClient();

    public async Task<RouteSnapAttemptResult> TrySnapAsync(RouteSession session, CancellationToken ct = default)
    {
        if (session.Points is not { Count: > 1 })
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: Connectivity.Current.NetworkAccess == NetworkAccess.Internet,
                message: "This route does not have enough points to snap.");
        }

        var hadInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        if (!hadInternet)
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: false,
                message: "No internet connection is available right now.");
        }

        // The public OSRM demo match endpoint rejects larger traces for some routes.
        var sampleSizes = new[] { 10, 8, 6 };

        RouteSnapAttemptResult? lastFailure = null;

        foreach (var sampleSize in sampleSizes)
        {
            ct.ThrowIfCancellationRequested();

            var attempt = await TrySnapWithSampleSizeAsync(session, sampleSize, ct);

            if (attempt.Success)
                return attempt;

            lastFailure = attempt;

            // If the failure is not "TooBig", don't keep shrinking blindly
            if (!attempt.Message.Contains("TooBig", StringComparison.OrdinalIgnoreCase) &&
                !attempt.Message.Contains("too many trace coordinates", StringComparison.OrdinalIgnoreCase))
            {
                return attempt;
            }
        }

        return lastFailure ?? RouteSnapAttemptResult.Fail(
            hadInternet: true,
            message: "Snapping failed after trying smaller route samples.");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DrivingSchoolApp/1.0 (+student project; contact not available)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private async Task<RouteSnapAttemptResult> TrySnapWithSampleSizeAsync(
        RouteSession session,
        int maxPoints,
        CancellationToken ct)
    {
        var sampled = SampleForMatch(session.Points, maxPoints);
        if (sampled.Count < 2)
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: true,
                message: "Not enough usable points were available after sampling.");
        }

        var coordinates = string.Join(";",
            sampled.Select(p =>
                $"{p.Longitude.ToString(CultureInfo.InvariantCulture)},{p.Latitude.ToString(CultureInfo.InvariantCulture)}"));

        var timestamps = string.Join(";",
            sampled.Select(p =>
                p.Timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));

        var radiuses = string.Join(";",
            sampled.Select(p =>
            {
                var accuracy = p.AccuracyMeters ?? 15;
                var snappedRadius = Math.Clamp(accuracy, 5, 25);
                return snappedRadius.ToString("0.##", CultureInfo.InvariantCulture);
            }));

        var url =
            $"https://router.project-osrm.org/match/v1/driving/{coordinates}" +
            $"?overview=full&geometries=geojson&timestamps={timestamps}&radiuses={radiuses}&steps=false&annotations=false&gaps=ignore&tidy=true";

        LogRequest(sampled.Count, url);

        Exception? lastException = null;
        System.Net.HttpStatusCode? lastStatusCode = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var response = await Http.GetAsync(url, ct);
                var responseText = await response.Content.ReadAsStringAsync(ct);
                LogResponse(sampled.Count, response.StatusCode, responseText);

                if (response.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(responseText);

                    if (!document.RootElement.TryGetProperty("matchings", out var matchings))
                    {
                        return RouteSnapAttemptResult.Fail(
                            hadInternet: true,
                            message: "The snapping service returned no matchings.");
                    }

                    var geometry = new List<RouteCoordinate>();

                    foreach (var matching in matchings.EnumerateArray())
                    {
                        if (!matching.TryGetProperty("geometry", out var geom))
                            continue;

                        if (!geom.TryGetProperty("coordinates", out var coords))
                            continue;

                        foreach (var coord in coords.EnumerateArray())
                        {
                            if (coord.GetArrayLength() < 2)
                                continue;

                            var lon = coord[0].GetDouble();
                            var lat = coord[1].GetDouble();

                            var next = new RouteCoordinate(lat, lon);

                            if (geometry.Count > 0)
                            {
                                var prev = geometry[^1];
                                if (Math.Abs(prev.Latitude - next.Latitude) < 0.0000001 &&
                                    Math.Abs(prev.Longitude - next.Longitude) < 0.0000001)
                                {
                                    continue;
                                }
                            }

                            geometry.Add(next);
                        }
                    }

                    if (geometry.Count < 2)
                    {
                        return RouteSnapAttemptResult.Fail(
                            hadInternet: true,
                            message: "The snapping service could not build a usable snapped route.");
                    }

                    var cache = new SnappedRouteCache
                    {
                        SessionId = session.Id,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        SourcePointCount = session.Points.Count,
                        Geometry = geometry
                    };

                    return RouteSnapAttemptResult.Ok(cache);
                }

                lastStatusCode = response.StatusCode;

                try
                {
                    using var errorDoc = JsonDocument.Parse(responseText);

                    var code = errorDoc.RootElement.TryGetProperty("code", out var codeProp)
                        ? codeProp.GetString()
                        : null;

                    var message = errorDoc.RootElement.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString()
                        : null;

                    if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(message))
                    {
                        return RouteSnapAttemptResult.Fail(
                            hadInternet: true,
                            message:
                                $"OSRM error: {code ?? "Unknown"}{(string.IsNullOrWhiteSpace(message) ? "" : $" - {message}")} (sampled {sampled.Count} points)");
                    }
                }
                catch
                {
                    // ignore body parse failure, fall back below
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                lastException = ex;
                LogException(sampled.Count, ex);
            }

            if (attempt < 3)
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
        }

        if (lastStatusCode is not null)
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: true,
                message: $"The snapping server responded with {(int)lastStatusCode} {lastStatusCode} (sampled {sampled.Count} points).");
        }

        if (lastException is HttpRequestException httpEx)
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: true,
                message: $"The snapping request failed: {httpEx.Message}");
        }

        if (lastException is TaskCanceledException)
        {
            return RouteSnapAttemptResult.Fail(
                hadInternet: true,
                message: "The snapping request timed out after several attempts.");
        }

        return RouteSnapAttemptResult.Fail(
            hadInternet: true,
            message: "Snapping failed after several attempts.");
    }

    private static List<TrackPoint> SampleForMatch(IReadOnlyList<TrackPoint> points, int maxPoints)
    {
        var ordered = points.OrderBy(p => p.Timestamp).ToList();

        if (ordered.Count <= maxPoints)
            return ordered;

        var result = new List<TrackPoint>(maxPoints);
        var step = (ordered.Count - 1d) / (maxPoints - 1d);

        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)Math.Round(i * step);
            if (index >= ordered.Count)
                index = ordered.Count - 1;

            var point = ordered[index];

            if (result.Count == 0 || result[^1].Timestamp != point.Timestamp)
                result.Add(point);
        }

        return result;
    }

    [Conditional("DEBUG")]
    private static void LogRequest(int sampleCount, string url)
    {
        Debug.WriteLine(
            $"OsrmRoadSnapService: request sampleCount={sampleCount}; urlLength={url.Length}; url={url}");
    }

    [Conditional("DEBUG")]
    private static void LogResponse(int sampleCount, System.Net.HttpStatusCode statusCode, string responseText)
    {
        var bodyPrefix = responseText.Length <= ResponseLogPrefixLength
            ? responseText
            : responseText[..ResponseLogPrefixLength];

        Debug.WriteLine(
            $"OsrmRoadSnapService: response sampleCount={sampleCount}; " +
            $"status={(int)statusCode} {statusCode}; bodyPrefix={bodyPrefix}");
    }

    [Conditional("DEBUG")]
    private static void LogException(int sampleCount, Exception exception)
    {
        Debug.WriteLine(
            $"OsrmRoadSnapService: request failed sampleCount={sampleCount}; " +
            $"{exception.GetType().Name}: {exception.Message}");
    }
}
