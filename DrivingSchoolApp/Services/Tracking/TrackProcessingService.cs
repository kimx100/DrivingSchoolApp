using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services.Tracking;

public sealed class TrackProcessingService
{
    private TrackPoint? _lastAccepted;
    private double _emaLat;
    private double _emaLon;
    private bool _emaInit;

    public void Reset()
    {
        _lastAccepted = null;
        _emaInit = false;
        _emaLat = 0;
        _emaLon = 0;
    }

    public bool TryProcess(TrackPoint raw, out TrackPoint processed)
    {
        processed = raw;
        var acc = raw.AccuracyMeters ?? 9999;

        // 1) Drop very inaccurate points
        if (acc > 50)
            return false;

        if (_lastAccepted is not null)
        {
            var prev = _lastAccepted;
            var dt = (raw.Timestamp - prev.Timestamp).TotalSeconds;

            if (dt <= 0.5)
                return false;

            var dist = HaversineMeters(prev.Latitude, prev.Longitude, raw.Latitude, raw.Longitude);
            var impliedSpeed = dist / dt;

            // 2) Drop impossible jumps
            if (dist > 250 && dt < 2.5)
                return false;

            if (impliedSpeed > 70)
                return false;

            // 3) Ignore tiny jitter
            if (dist < 2)
                return false;
        }

        // 4) EMA smoothing
        var speed = raw.SpeedMps ?? 0;
        var alpha = 0.12 + Clamp(speed / 30.0, 0, 1) * 0.38;

        if (acc > 20)
            alpha *= 0.7;

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

        processed = raw with
        {
            Latitude = _emaLat,
            Longitude = _emaLon
        };

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