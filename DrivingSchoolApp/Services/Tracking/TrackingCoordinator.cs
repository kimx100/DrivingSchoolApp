using System.Linq;
using DrivingSchoolApp.Models;
using Microsoft.Maui.Storage;

namespace DrivingSchoolApp.Services.Tracking;

public sealed class TrackingCoordinator
{
    public static TrackingCoordinator Instance { get; } = new();

    private readonly object _gate = new();
    private readonly TrackProcessingService _processor = new();
    private readonly List<TrackPoint> _allPoints = new();

    private bool _isTracking;
    private bool _hasActiveSession;
    private DateTimeOffset? _sessionStartedAt;
    private DateTimeOffset? _currentRunStartedAt;
    private TimeSpan _elapsedBeforeCurrentRun;
    private TrackPoint? _latestPoint;

    private TrackingCoordinator()
    {
    }

    public event Action<TrackingSnapshot>? SnapshotChanged;
    public event Action<TrackPoint>? PointAccepted;

    public TrackingSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return BuildSnapshotLocked();
        }
    }

    public IReadOnlyList<TrackPoint> GetRecentPointsNewestFirst(int maxCount)
    {
        lock (_gate)
        {
            return _allPoints
                .OrderByDescending(p => p.Timestamp)
                .Take(maxCount)
                .ToList();
        }
    }

    public IReadOnlyList<TrackPoint> GetAllPointsOldestFirst()
    {
        lock (_gate)
        {
            return _allPoints
                .OrderBy(p => p.Timestamp)
                .ToList();
        }
    }

    public bool StartOrResumeSession()
    {
        TrackingSnapshot snapshot;

        lock (_gate)
        {
            if (_isTracking)
                return false;

            if (!_hasActiveSession)
            {
                _allPoints.Clear();
                _processor.Reset();

                _hasActiveSession = true;
                _sessionStartedAt = DateTimeOffset.UtcNow;
                _elapsedBeforeCurrentRun = TimeSpan.Zero;
                _latestPoint = null;
            }

            _currentRunStartedAt = DateTimeOffset.UtcNow;
            _isTracking = true;

            snapshot = BuildSnapshotLocked();
        }

        SnapshotChanged?.Invoke(snapshot);
        return true;
    }

    public bool PauseSession()
    {
        TrackingSnapshot snapshot;

        lock (_gate)
        {
            if (!_isTracking)
                return false;

            AccumulateElapsedLocked(DateTimeOffset.UtcNow);
            _currentRunStartedAt = null;
            _isTracking = false;

            snapshot = BuildSnapshotLocked();
        }

        SnapshotChanged?.Invoke(snapshot);
        return true;
    }

    public bool TryAcceptRawPoint(TrackPoint raw, out TrackPoint processed)
    {
        processed = raw;

        TrackPoint accepted;
        TrackingSnapshot snapshot;

        lock (_gate)
        {
            if (!_isTracking)
                return false;

            if (!_processor.TryProcess(raw, out accepted))
                return false;

            _allPoints.Add(accepted);
            _latestPoint = accepted;
            processed = accepted;

            snapshot = BuildSnapshotLocked();
        }

        PointAccepted?.Invoke(accepted);
        SnapshotChanged?.Invoke(snapshot);
        return true;
    }

    public async Task<RouteSession?> EndAndSaveAsync()
    {
        RouteSession? session = null;
        TrackingSnapshot snapshot;
        var endedAt = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            if (!_hasActiveSession)
                return null;

            if (_isTracking)
            {
                AccumulateElapsedLocked(endedAt);
                _currentRunStartedAt = null;
                _isTracking = false;
            }

            var ordered = _allPoints.OrderBy(p => p.Timestamp).ToList();

            if (ordered.Count > 1)
            {
                session = new RouteSession
                {
                    StartedAt = _sessionStartedAt ?? ordered.First().Timestamp,
                    EndedAt = endedAt,
                    Points = ordered
                };
            }

            ResetStateLocked(clearPoints: true, resetProcessor: true);
            snapshot = BuildSnapshotLocked();
        }

        if (session is not null)
        {
            await RouteStorage.SaveAsync(session);
            Preferences.Set("LastRouteSessionId", session.Id);
        }

        SnapshotChanged?.Invoke(snapshot);
        return session;
    }

    public void ClearSession()
    {
        TrackingSnapshot snapshot;

        lock (_gate)
        {
            ResetStateLocked(clearPoints: true, resetProcessor: true);
            snapshot = BuildSnapshotLocked();
        }

        SnapshotChanged?.Invoke(snapshot);
    }

    private void AccumulateElapsedLocked(DateTimeOffset now)
    {
        if (_currentRunStartedAt is null)
            return;

        _elapsedBeforeCurrentRun += now - _currentRunStartedAt.Value;
    }

    private TimeSpan GetElapsedLocked(DateTimeOffset now)
    {
        var elapsed = _elapsedBeforeCurrentRun;

        if (_isTracking && _currentRunStartedAt is not null)
            elapsed += now - _currentRunStartedAt.Value;

        return elapsed;
    }

    private void ResetStateLocked(bool clearPoints, bool resetProcessor)
    {
        _isTracking = false;
        _hasActiveSession = false;
        _sessionStartedAt = null;
        _currentRunStartedAt = null;
        _elapsedBeforeCurrentRun = TimeSpan.Zero;
        _latestPoint = null;

        if (clearPoints)
            _allPoints.Clear();

        if (resetProcessor)
            _processor.Reset();
    }

    private TrackingSnapshot BuildSnapshotLocked()
        => new(
            IsTracking: _isTracking,
            HasActiveSession: _hasActiveSession,
            SessionStartedAt: _sessionStartedAt,
            Elapsed: GetElapsedLocked(DateTimeOffset.UtcNow),
            TotalPointCount: _allPoints.Count,
            LatestPoint: _latestPoint
        );
}