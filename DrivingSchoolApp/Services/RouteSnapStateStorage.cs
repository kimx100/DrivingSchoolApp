using System.Text.Json;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class RouteSnapStateStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string StateDir =>
        Path.Combine(FileSystem.AppDataDirectory, "route-snap-states");

    public static async Task SaveAsync(RouteSnapState state)
    {
        Directory.CreateDirectory(StateDir);
        var path = Path.Combine(StateDir, $"{state.SessionId}.state.json");
        var json = JsonSerializer.Serialize(state, Options);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<RouteSnapState?> LoadAsync(string sessionId)
    {
        var path = Path.Combine(StateDir, $"{sessionId}.state.json");
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<RouteSnapState>(json, Options);
    }

    public static Task DeleteAsync(string sessionId)
    {
        var path = Path.Combine(StateDir, $"{sessionId}.state.json");
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}