using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.Services;
using DrivingSchoolApp.Services.API;

namespace DrivingSchoolApp.Pages;

public partial class MyPage : ContentPage
{
    private bool _loaded;
    private readonly IAuthService _authService;
    private readonly IDrivingSchoolService _drivingSchoolService;

    public MyPage(IAuthService authService, IDrivingSchoolService drivingSchoolService)
    {
        _authService = authService;
        _drivingSchoolService = drivingSchoolService;
        
        InitializeComponent();
        LoadPriceFields();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        LoadPriceFields();

        if (!_loaded)
            Dispatcher.Dispatch(async () => await LoadProfileAsync());
    }

    private async Task LoadProfileAsync()
    {
        ProfileStatusLabel.Text = "Indlæser profil...";

        try
        {
            var instructorResult = await _authService.GetSelfAsync<InstructorDto>(false);
            if (!instructorResult.IsSuccessful || instructorResult.Data is null)
                throw new InvalidOperationException("Kunne ikke hente underviserprofil.");

            var instructor = instructorResult.Data;
            InstructorNameLabel.Text = $"{instructor.Name.FirstName} {instructor.Name.LastName}".Trim();
            InstructorEmailLabel.Text = instructor.EmailAddress;
            InstructorPhoneLabel.Text = instructor.PhoneNumber;
            DrivingSchoolLabel.Text = instructor.SchoolId.ToString();

            var schoolResult = await _drivingSchoolService.GetDrivingSchoolByIdAsync(instructor.SchoolId);
            if (schoolResult.IsSuccessful && schoolResult.Data is not null)
                DrivingSchoolLabel.Text = schoolResult.Data.Name;

            ProfileStatusLabel.Text = string.Empty;
            _loaded = true;
        }
        catch (Exception ex)
        {
#if DEBUG
            ProfileStatusLabel.Text = $"Kunne ikke hente profil. {ex.GetType().Name}: {ex.Message}";
#else
            ProfileStatusLabel.Text = "Kunne ikke hente profil.";
#endif
        }
    }

    private void LoadPriceFields()
    {
        DrivingLessonPriceEntry.Text = LessonPriceSettingsService.FormatPrice(
            LessonPriceSettingsService.GetDefaultDrivingLessonPrice());
        TheoryLessonPriceEntry.Text = LessonPriceSettingsService.FormatPrice(
            LessonPriceSettingsService.GetDefaultTheoryLessonPrice());
    }

    private void SavePricesButton_Clicked(object? sender, EventArgs e)
    {
        DrivingLessonPriceEntry.Unfocus();
        TheoryLessonPriceEntry.Unfocus();

        if (!LessonPriceSettingsService.TryParsePrice(DrivingLessonPriceEntry.Text, out var drivingPrice))
        {
            PriceStatusLabel.Text = "Angiv en gyldig standardpris for kørelektion.";
            return;
        }

        if (!LessonPriceSettingsService.TryParsePrice(TheoryLessonPriceEntry.Text, out var theoryPrice))
        {
            PriceStatusLabel.Text = "Angiv en gyldig standardpris for teorilektion.";
            return;
        }

        LessonPriceSettingsService.SaveDefaultPrices(drivingPrice, theoryPrice);
        PriceStatusLabel.Text = "Priser gemt.";
    }

    private void PriceEntry_Completed(object? sender, EventArgs e)
    {
        if (sender is Entry entry)
            entry.Unfocus();
    }

    private async void LogoutButton_Clicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync("Log ud", "Vil du logge ud?", "Log ud", "Annuller");
        if (!confirmed)
            return;

        try
        {
            await _authService.LogoutAsync();
            await AppNavigation.OpenLogInPageAsync(_authService);
        }
        catch
        {
            await DisplayAlertAsync("Log ud", "Kunne ikke logge ud. Prøv igen.", "OK");
        }
    }
}
