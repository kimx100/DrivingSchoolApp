using DrivingSchoolApp.Pages;

namespace DrivingSchoolApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LiveRoutePage), typeof(LiveRoutePage));
    }
}