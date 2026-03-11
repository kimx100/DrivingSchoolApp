using System.Text.Json;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class RouteStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string RoutesDir =>
        Path.Combine(FileSystem.AppDataDirectory, "routes");

    public static async Task SaveAsync(RouteSession session)
    {
        Directory.CreateDirectory(RoutesDir);
        var path = Path.Combine(RoutesDir, $"{session.Id}.json");
        var json = JsonSerializer.Serialize(session, Options);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<RouteSession?> LoadAsync(string sessionId)
    {
        var path = Path.Combine(RoutesDir, $"{sessionId}.json");
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<RouteSession>(json, Options);
    }
}