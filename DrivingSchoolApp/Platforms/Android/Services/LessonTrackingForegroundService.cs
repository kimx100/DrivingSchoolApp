#if ANDROID
using AndroidX.Core.App;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services.Tracking;
using Microsoft.Maui.Devices.Sensors;

namespace DrivingSchoolApp.Platforms.Android.Services;

[global::Android.App.Service(
    Exported = false,
    ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class LessonTrackingForegroundService : global::Android.App.Service
{
    public const string ActionStart = "DrivingSchoolApp.action.START_LESSON_TRACKING";
    public const string ActionStop = "DrivingSchoolApp.action.STOP_LESSON_TRACKING";

    private const string ChannelId = "lesson_tracking_channel";
    private const int NotificationId = 11001;

    private CancellationTokenSource? _cts;
    private Task? _trackingTask;
    private bool _isRunning;

    public static Task<bool> StartServiceAsync()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(context, typeof(LessonTrackingForegroundService));
            intent.SetAction(ActionStart);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public static Task StopServiceAsync()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(context, typeof(LessonTrackingForegroundService));
            intent.SetAction(ActionStop);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }
        catch
        {
            // ignore stop request failures
        }

        return Task.CompletedTask;
    }

    public override global::Android.OS.IBinder? OnBind(global::Android.Content.Intent? intent) => null;

    public override global::Android.App.StartCommandResult OnStartCommand(
        global::Android.Content.Intent? intent,
        global::Android.App.StartCommandFlags flags,
        int startId)
    {
        var action = intent?.Action ?? ActionStart;

        if (action == ActionStop)
        {
            StopTrackingLoop();
            return global::Android.App.StartCommandResult.NotSticky;
        }

        if (_isRunning)
            return global::Android.App.StartCommandResult.NotSticky;

        EnsureNotificationChannel();
        StartForegroundTrackingNotification();

        _cts = new CancellationTokenSource();
        _trackingTask = RunTrackingLoopAsync(_cts.Token);
        _isRunning = true;

        return global::Android.App.StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _trackingTask = null;
        _isRunning = false;

        base.OnDestroy();
    }

    private async Task RunTrackingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

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

                var raw = new TrackPoint(
                    Timestamp: DateTimeOffset.UtcNow,
                    Latitude: location.Latitude,
                    Longitude: location.Longitude,
                    AccuracyMeters: location.Accuracy,
                    SpeedMps: location.Speed
                );

                if (TrackingCoordinator.Instance.TryAcceptRawPoint(raw, out _))
                {
                    var snapshot = TrackingCoordinator.Instance.GetSnapshot();
                    UpdateNotification(snapshot.TotalPointCount);
                }
            }
            catch (System.OperationCanceledException)
            {
                break;
            }
            catch
            {
                // keep running even if one tick fails
            }
        }
    }

    private void StopTrackingLoop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _trackingTask = null;
        _isRunning = false;

        StopForeground(global::Android.App.StopForegroundFlags.Remove);
        StopSelf();
    }

    private void StartForegroundTrackingNotification()
    {
        var notification = BuildNotification("Tracking lesson route...");

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Q)
        {
            StartForeground(
                NotificationId,
                notification,
                global::Android.Content.PM.ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }
    }

    private void UpdateNotification(int pointCount)
    {
        var notification = BuildNotification($"Tracking active - Points: {pointCount}");
        var manager = NotificationManagerCompat.From(this);
        manager.Notify(NotificationId, notification);
    }

    private global::Android.App.Notification BuildNotification(string text)
    {
        var openAppIntent = new global::Android.Content.Intent(this, typeof(MainActivity));
        openAppIntent.AddFlags(
            global::Android.Content.ActivityFlags.SingleTop |
            global::Android.Content.ActivityFlags.ClearTop |
            global::Android.Content.ActivityFlags.NewTask);

        var pendingIntent = global::Android.App.PendingIntent.GetActivity(
            this,
            2001,
            openAppIntent,
            global::Android.App.PendingIntentFlags.Immutable |
            global::Android.App.PendingIntentFlags.UpdateCurrent);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Driving lesson tracking")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogMap)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetContentIntent(pendingIntent)
            .SetCategory(NotificationCompat.CategoryService)
            .Build();
    }

    private void EnsureNotificationChannel()
    {
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.O)
            return;

        var manager = GetSystemService(global::Android.Content.Context.NotificationService) as global::Android.App.NotificationManager;
        if (manager is null)
            return;

        var existing = manager.GetNotificationChannel(ChannelId);
        if (existing is not null)
            return;

        var channel = new global::Android.App.NotificationChannel(
            ChannelId,
            "Lesson tracking",
            global::Android.App.NotificationImportance.Low)
        {
            Description = "Shows when a driving lesson route is being tracked."
        };

        manager.CreateNotificationChannel(channel);
    }
}
#endif