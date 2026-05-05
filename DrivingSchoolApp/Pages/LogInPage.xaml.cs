using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DrivingSchoolApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace DrivingSchoolApp.Pages;

public partial class LogInPage : ContentPage
{
    public LogInPage()
    {
        InitializeComponent();
        BindingContext = new LogInViewModel();

#if DEBUG
        AddDebugSkipButton();
#endif
    }

#if DEBUG
    private void AddDebugSkipButton()
    {
        var skipButton = new Button
        {
            Text = "Continue without login",
            CornerRadius = 12
        };

        skipButton.SetDynamicResource(Button.BackgroundColorProperty, "Gray300");
        skipButton.SetDynamicResource(Button.TextColorProperty, "Black");
        skipButton.Clicked += ContinueWithoutLoginButton_Clicked;

        LoginFormLayout.Children.Add(skipButton);
    }

    private static async void ContinueWithoutLoginButton_Clicked(object? sender, EventArgs e)
    {
        await AppNavigation.OpenAppShellAsync();
    }
#endif
}

internal static class AppNavigation
{
    public static Task OpenAppShellAsync()
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            var window = Application.Current?.Windows.FirstOrDefault();

            if (window is not null)
            {
                window.Page = new global::DrivingSchoolApp.AppShell();
            }
        });
    }
}

internal sealed class LogInViewModel : BindableObject
{
    private readonly AuthService _authService = new();
    private string _username = string.Empty;
    private string _password = string.Empty;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand LoginCommand { get; }

    public LogInViewModel()
    {
        LoginCommand = new Command(async () => await ExecuteLoginAsync());
    }

    private async Task ExecuteLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await DisplayAlertAsync("Missing information", "Please provide both email and password.", "OK");
            return;
        }
        
        try
        {
            await _authService.LoginInstructorAsync(Username.Trim(), Password);

            await AppNavigation.OpenAppShellAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Login failed", ex.Message, "OK");
        }
    }

    private static Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlertAsync(title, message, cancel) ?? Task.CompletedTask;
    }
    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
