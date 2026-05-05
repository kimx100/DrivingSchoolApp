using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.DTOs.Common;
using RestSharp;

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

    private static void ContinueWithoutLoginButton_Clicked(object? sender, EventArgs e)
    {
        var window = Application.Current?.Windows.FirstOrDefault();

        if (window is not null)
        {
            window.Page = new global::DrivingSchoolApp.AppShell();
        }
    }
#endif
}

internal sealed class LogInViewModel : BindableObject
{
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
            await DisplayAlertAsync(AppText.LoginMissingInfoTitle, AppText.LoginMissingInfoMessage, AppText.CommonOk);
            return;
        }
        
        var client = new RestClient("http://10.115.248.247:5259");
        var request = new RestRequest("/auth/login/instructor", Method.Post);
        request.AddBody(new LoginDto(Username, Password));
        
        var response = await client.ExecuteAsync<JwtTokenDto>(request);
 
        if(!response.IsSuccessful)
            await DisplayAlertAsync("Login", response.StatusCode.ToString(), "OK");
        else
            await DisplayAlertAsync("Login", "Login Successful", "OK");

        await DisplayAlertAsync(AppText.LoginSuccessTitle, AppText.LoginSuccessMessage, AppText.LoginSuccessButton);
    }

    private static Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlertAsync(title, message, cancel) ?? Task.CompletedTask;
    }
// http://10.0.2.2
//port:5259
//William47@gmail.com
//password: test1234
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
