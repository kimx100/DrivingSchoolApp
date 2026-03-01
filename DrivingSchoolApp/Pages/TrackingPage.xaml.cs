namespace DrivingSchoolApp.Pages;

public partial class TrackingPage : ContentPage
{
    private bool _isRunning;

    public TrackingPage()
    {
        InitializeComponent();
    }

    private async void Start_Clicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Status: requesting permission…";

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Status: permission denied";
            return;
        }

        StatusLabel.Text = "Status: reading location…";

        var location = await Geolocation.Default.GetLastKnownLocationAsync()
                       ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));

        if (location is null)
        {
            StatusLabel.Text = "Status: no location yet";
            return;
        }

        _isRunning = true;
        StatusLabel.Text = $"Status: OK ({location.Latitude:F6}, {location.Longitude:F6})";
    }

    private void Stop_Clicked(object sender, EventArgs e)
    {
        _isRunning = false;
        StatusLabel.Text = "Status: stopped";
    }
}