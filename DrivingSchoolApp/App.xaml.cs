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
        return new Window(new LogInPage(_authService));
    }
}
