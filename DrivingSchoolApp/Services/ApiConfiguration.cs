namespace DrivingSchoolApp.Services;

public static class ApiConfiguration
{
    // TODO: Move this into environment-specific app configuration.
    // Android emulator: use http://10.0.2.2:5259 to reach the host machine.
    // Physical Android phone: use the PC LAN/hotspot IP from ipconfig.
    // If the PC changes network, this IP can change and must be updated.
    public const string BaseUrl = "http://10.115.248.247:5259";
}
