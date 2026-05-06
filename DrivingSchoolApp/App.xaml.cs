using DrivingSchoolApp.Pages;
using DrivingSchoolApp.Services.API;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingSchoolApp;

public partial class App : Application
{
    private readonly IAuthService _authService;
    public App(IAuthService authService)
    {
        InitializeComponent();
        
        _authService = authService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new ContentPage());
        _ = SetInitialPageAsync(window);
        return window;
    }

    private async Task SetInitialPageAsync(Window window)
    {
        try
        {
            window.Page = await _authService.HasSavedAccessTokenAsync()
                ? new AppShell()
                : new LogInPage(_authService);
        }
        catch
        {
            window.Page = new LogInPage(_authService);
        }
    }
}
