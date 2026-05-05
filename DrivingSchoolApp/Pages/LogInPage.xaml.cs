using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Services.API;

namespace DrivingSchoolApp.Pages;

public partial class LogInPage : ContentPage
{
    public LogInPage(IAuthService authService)
    {
        InitializeComponent();
        BindingContext = new LogInViewModel(authService);
    }
}

internal sealed class LogInViewModel : BindableObject
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private readonly IAuthService _authService;
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

    public LogInViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await ExecuteLoginAsync());
    }

    private async Task ExecuteLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await DisplayAlertAsync(AppText.LoginMissingInfoTitle, AppText.LoginMissingInfoMessage, AppText.CommonOk);
            return;
        }

        var loginDto = new LoginDto(Username, Password);
        var successfulLogin = await _authService.LoginInstructorAsync(loginDto);

        if(successfulLogin) 
            await DisplayAlertAsync(AppText.LoginSuccessTitle, AppText.LoginSuccessMessage, AppText.LoginSuccessButton);
        else
        {
            // TODO: Find a proper error message
            await DisplayAlertAsync(AppText.LoginMissingInfoTitle, AppText.LoginMissingInfoMessage, AppText.CommonOk);
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
