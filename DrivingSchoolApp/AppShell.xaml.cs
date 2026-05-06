using DrivingSchoolApp.Pages;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.Services.Tracking;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

#if ANDROID
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
#endif

namespace DrivingSchoolApp;

public partial class AppShell : Shell
{
    private readonly TrackingCoordinator _trackingCoordinator = TrackingCoordinator.Instance;

    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(LiveRoutePage), typeof(LogInPage));
        Routing.RegisterRoute(nameof(LogInPage), typeof(LogInPage));
        Routing.RegisterRoute(nameof(RouteDetailPage), typeof(RouteDetailPage));
        Routing.RegisterRoute(nameof(RouteConfirmationPage), typeof(RouteConfirmationPage));
        Routing.RegisterRoute(nameof(SavedRoutesPage), typeof(SavedRoutesPage));
        Routing.RegisterRoute(nameof(MyPage), typeof(MyPage));
        
        _trackingCoordinator.SnapshotChanged += TrackingCoordinator_SnapshotChanged;
        ApplyChrome(_trackingCoordinator.GetSnapshot().IsTracking);

        _ = RouteSnapBackgroundProcessor.EnsureWorkQueuedForAllRoutesAsync();
    }

    private void TrackingCoordinator_SnapshotChanged(TrackingSnapshot snapshot)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyChrome(snapshot.IsTracking);
        });
    }

    private void ApplyChrome(bool isTracking)
    {
        var chromeBackground = ResolveColor("ChromeBackground", Color.FromArgb("#2F6FD6"));
        var statusBarBackground = ResolveColor(
            isTracking ? "ActiveRouteChromeBackground" : "ChromeBackground",
            isTracking ? Color.FromArgb("#2F9B63") : Color.FromArgb("#2F6FD6"));
        var foreground = ResolveColor("ChromeForeground", Colors.White);
        var unselected = ResolveColor("ChromeUnselected", Color.FromArgb("#DCE7FB"));
        var disabled = ResolveColor("Gray300", Color.FromArgb("#E2E2E2"));

        Shell.SetBackgroundColor(this, chromeBackground);
        Shell.SetForegroundColor(this, foreground);
        Shell.SetTitleColor(this, foreground);
        Shell.SetUnselectedColor(this, unselected);
        Shell.SetDisabledColor(this, disabled);
        Shell.SetTabBarBackgroundColor(this, chromeBackground);
        Shell.SetTabBarForegroundColor(this, foreground);
        Shell.SetTabBarTitleColor(this, foreground);
        Shell.SetTabBarUnselectedColor(this, unselected);

        ApplyPlatformChrome(statusBarBackground, chromeBackground);
    }

    private static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true &&
            value is Color color)
        {
            return color;
        }

        return fallback;
    }

    private static void ApplyPlatformChrome(Color statusBarBackground, Color navigationBarBackground)
    {
#if ANDROID
        var window = Platform.CurrentActivity?.Window;
        if (window is null)
            return;

        window.SetStatusBarColor(statusBarBackground.ToPlatform());
        window.SetNavigationBarColor(navigationBarBackground.ToPlatform());

        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is null)
            return;

        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = false;
#endif
    }
}
