using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DrivingSchoolApp.Localization;

namespace DrivingSchoolApp.Pages;

public partial class LogInPage : ContentPage
{
    public LogInPage()
    {
        InitializeComponent();
        BindingContext = new LogInViewModel();
    }
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

        await DisplayAlertAsync(AppText.LoginSuccessTitle, AppText.LoginSuccessMessage, AppText.LoginSuccessButton);
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
