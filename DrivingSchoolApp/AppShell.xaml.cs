using DrivingSchoolApp.Pages;
using DrivingSchoolApp.Services;

namespace DrivingSchoolApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RouteDetailPage), typeof(RouteDetailPage));

        _ = RouteSnapBackgroundProcessor.EnsureWorkQueuedForAllRoutesAsync();
    }
}