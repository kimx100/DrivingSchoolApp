namespace DrivingSchoolApp.Services;

public static class ApiConfiguration
{
    // TODO: Replace this with the confirmed real dev API URL.
    // 10.0.2.2 reaches the host machine from the Android emulator.
    // A physical Android phone cannot use localhost/10.0.2.2 to reach the laptop API;
    // use the laptop LAN IP or a hosted/tunnel URL for device testing.
    public const string BaseUrl = "http://10.0.2.2:5259";
}
