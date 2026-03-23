using System.Text.Json;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class RouteSnapStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string SnapDir =>
        Path.Combine(FileSystem.AppDataDirectory, "route-snaps");

    public static async Task SaveAsync(SnappedRouteCache cache)
    {
        Directory.CreateDirectory(SnapDir);
        var path = Path.Combine(SnapDir, $"{cache.SessionId}.snap.json");
        var json = JsonSerializer.Serialize(cache, Options);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<SnappedRouteCache?> LoadAsync(string sessionId)
    {
        var path = Path.Combine(SnapDir, $"{sessionId}.snap.json");
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<SnappedRouteCache>(json, Options);
    }

    public static Task DeleteAsync(string sessionId)
    {
        var path = Path.Combine(SnapDir, $"{sessionId}.snap.json");

        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}