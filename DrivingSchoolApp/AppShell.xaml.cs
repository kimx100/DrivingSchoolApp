namespace DrivingSchoolApp;
using DrivingSchoolApp.Pages;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
    }
}