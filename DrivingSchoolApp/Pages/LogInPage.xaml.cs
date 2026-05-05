using System.Linq;
using System.Threading.Tasks;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace DrivingSchoolApp.Pages;

public partial class LogInPage : ContentPage
{
    private readonly AuthService _authService = new();
    private bool _isLoggingIn;

    public LogInPage()
    {
        InitializeComponent();

#if DEBUG
        AddDebugSkipButton();
#endif
    }

    private async void LoginButton_Clicked(object? sender, EventArgs e)
    {
        if (_isLoggingIn)
            return;

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync(AppText.LoginMissingInfoTitle, AppText.LoginMissingInfoMessage, AppText.CommonOk);
            return;
        }

        try
        {
            SetLoginBusy(true);
            await _authService.LoginInstructorAsync(email, password);
            await AppNavigation.OpenAppShellAsync();
        }
        catch (Exception ex)
        {
            await DisplayLoginErrorAsync(ex);
        }
        finally
        {
            SetLoginBusy(false);
        }
    }

    private void SetLoginBusy(bool isBusy)
    {
        _isLoggingIn = isBusy;
        LoginButton.IsEnabled = !isBusy;
        LoginButton.Text = isBusy ? "Logging in..." : AppText.LoginButton;
    }

    private Task DisplayLoginErrorAsync(Exception exception)
    {
        var message = IsApiReachabilityError(exception)
            ? "Could not reach the API. Check that the API is running and that BaseUrl points to an address reachable from this phone."
            : exception.Message;

#if DEBUG
        message = $"{message}{Environment.NewLine}{Environment.NewLine}BaseUrl: {ApiConfiguration.BaseUrl}{Environment.NewLine}Details: {exception.GetType().Name}: {exception.Message}";
#else
        message = exception is InvalidOperationException
            ? message
            : "Login failed. Please try again.";
#endif

        return DisplayAlertAsync("Login failed", message, AppText.CommonOk);
    }

    private static bool IsApiReachabilityError(Exception exception)
    {
        var message = exception.ToString();
        return message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ETIMEDOUT", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase)
            || message.Contains("No route to host", StringComparison.OrdinalIgnoreCase)
            || message.Contains("API is running and reachable", StringComparison.OrdinalIgnoreCase);
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
