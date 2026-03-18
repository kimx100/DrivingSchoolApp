using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services.Tracking;

public static class LessonTrackingPlatform
{
    public static Task<bool> StartAsync()
    {
#if ANDROID
        return DrivingSchoolApp.Platforms.Android.Services.LessonTrackingForegroundService.StartServiceAsync();
#else
        return Task.FromResult(false);
#endif
    }

    public static Task StopAsync()
    {
#if ANDROID
        return DrivingSchoolApp.Platforms.Android.Services.LessonTrackingForegroundService.StopServiceAsync();
#else
        return Task.CompletedTask;
#endif
    }
}