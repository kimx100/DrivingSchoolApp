using System.Linq;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class RouteSnapBackgroundProcessor
{
    private static readonly object QueueGate = new();
    private static readonly HashSet<string> QueuedSessionIds = new();
    private static readonly SemaphoreSlim PumpLock = new(1, 1);
    private static readonly OsrmRoadSnapService SnapService = new();

    public static event Action? StatesChanged;

    public static async Task EnsureWorkQueuedForAllRoutesAsync()
    {
        var routes = await RouteStorage.ListAsync();

        foreach (var route in routes)
        {
            var state = await RouteSnapStateStorage.LoadAsync(route.Id);
            var snapCache = await RouteSnapStorage.LoadAsync(route.Id);

            if (HasUsableSnap(route, snapCache))
            {
                if (state is null)
                {
                    state = new RouteSnapState
                    {
                        SessionId = route.Id,
                        Status = RouteSnapWorkStatus.Viewed,
                        HasBeenViewed = true,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    };

                    await RouteSnapStateStorage.SaveAsync(state);
                }

                continue;
            }

            if (state is null)
            {
                state = new RouteSnapState
                {
                    SessionId = route.Id,
                    Status = RouteSnapWorkStatus.Pending,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                await RouteSnapStateStorage.SaveAsync(state);
                QueueSessionInternal(route.Id);
            }
            else if (state.Status is RouteSnapWorkStatus.Pending or RouteSnapWorkStatus.Processing)
            {
                if (state.Status == RouteSnapWorkStatus.Processing)
                {
                    state.Status = RouteSnapWorkStatus.Pending;
                    state.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    await RouteSnapStateStorage.SaveAsync(state);
                }

                QueueSessionInternal(route.Id);
            }
        }

        RaiseStatesChanged();
        _ = RunQueueAsync();
    }

    public static async Task EnqueueAsync(string sessionId)
    {
        var state = await RouteSnapStateStorage.LoadAsync(sessionId) ?? new RouteSnapState
        {
            SessionId = sessionId
        };

        state.Status = RouteSnapWorkStatus.Pending;
        state.LastError = null;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RouteSnapStateStorage.SaveAsync(state);

        QueueSessionInternal(sessionId);
        RaiseStatesChanged();
        _ = RunQueueAsync();
    }

    public static Task<RouteSnapState?> GetStateAsync(string sessionId)
        => RouteSnapStateStorage.LoadAsync(sessionId);

    public static async Task MarkViewedAsync(string sessionId)
    {
        var state = await RouteSnapStateStorage.LoadAsync(sessionId);
        if (state is null)
            return;

        state.HasBeenViewed = true;
        state.Status = RouteSnapWorkStatus.Viewed;
        state.LastError = null;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RouteSnapStateStorage.SaveAsync(state);
        RaiseStatesChanged();
    }

    public static async Task MarkManualSnapSucceededAsync(string sessionId)
    {
        var state = await RouteSnapStateStorage.LoadAsync(sessionId) ?? new RouteSnapState
        {
            SessionId = sessionId
        };

        state.HasBeenViewed = true;
        state.Status = RouteSnapWorkStatus.Viewed;
        state.LastError = null;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RouteSnapStateStorage.SaveAsync(state);
        RaiseStatesChanged();
    }

    private static async Task RunQueueAsync()
    {
        if (!await PumpLock.WaitAsync(0))
            return;

        try
        {
            while (true)
            {
                string? nextSessionId = null;

                lock (QueueGate)
                {
                    if (QueuedSessionIds.Count > 0)
                    {
                        nextSessionId = QueuedSessionIds.First();
                        QueuedSessionIds.Remove(nextSessionId);
                    }
                }

                if (nextSessionId is null)
                    break;

                await ProcessOneAsync(nextSessionId);
            }
        }
        finally
        {
            PumpLock.Release();

            lock (QueueGate)
            {
                if (QueuedSessionIds.Count > 0)
                    _ = RunQueueAsync();
            }
        }
    }

    private static async Task ProcessOneAsync(string sessionId)
    {
        var state = await RouteSnapStateStorage.LoadAsync(sessionId) ?? new RouteSnapState
        {
            SessionId = sessionId
        };

        var route = await RouteStorage.LoadAsync(sessionId);
        if (route is null || route.Points is not { Count: > 1 })
        {
            state.Status = RouteSnapWorkStatus.Failed;
            state.LastError = "Route file could not be loaded.";
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await RouteSnapStateStorage.SaveAsync(state);
            RaiseStatesChanged();
            return;
        }

        var existingSnap = await RouteSnapStorage.LoadAsync(sessionId);
        if (HasUsableSnap(route, existingSnap))
        {
            state.Status = state.HasBeenViewed
                ? RouteSnapWorkStatus.Viewed
                : RouteSnapWorkStatus.ReadyToView;
            state.LastError = null;
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await RouteSnapStateStorage.SaveAsync(state);
            RaiseStatesChanged();
            return;
        }

        state.Status = RouteSnapWorkStatus.Processing;
        state.LastError = null;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RouteSnapStateStorage.SaveAsync(state);
        RaiseStatesChanged();

        var result = await SnapService.TrySnapAsync(route);

        if (result.Success && result.Cache is not null)
        {
            await RouteSnapStorage.SaveAsync(result.Cache);

            state.Status = state.HasBeenViewed
                ? RouteSnapWorkStatus.Viewed
                : RouteSnapWorkStatus.ReadyToView;
            state.LastError = null;
        }
        else
        {
            state.Status = RouteSnapWorkStatus.Failed;
            state.LastError = result.Message;
        }

        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RouteSnapStateStorage.SaveAsync(state);
        RaiseStatesChanged();
    }

    private static bool HasUsableSnap(RouteSession route, SnappedRouteCache? snap)
    {
        return snap is not null &&
               snap.SessionId == route.Id &&
               snap.SourcePointCount == route.Points.Count &&
               snap.Geometry is { Count: > 1 };
    }

    private static void QueueSessionInternal(string sessionId)
    {
        lock (QueueGate)
        {
            QueuedSessionIds.Add(sessionId);
        }
    }

    private static void RaiseStatesChanged()
        => StatesChanged?.Invoke();
}