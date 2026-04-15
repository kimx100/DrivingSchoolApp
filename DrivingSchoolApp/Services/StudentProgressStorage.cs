using System.Text.Json;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class StudentProgressStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string StudentsPath =>
        Path.Combine(FileSystem.AppDataDirectory, "students.json");

    public static async Task<StudentRecord?> GetByIdAsync(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return null;

        var students = await LoadAllAsync();
        return students.FirstOrDefault(x => string.Equals(x.Id, studentId, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<StudentRecord?> FindByNameAsync(string? fullName)
    {
        var normalized = NormalizeName(fullName);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var students = await LoadAllAsync();

        return students.FirstOrDefault(x =>
            string.Equals(NormalizeName(x.FullName), normalized, StringComparison.Ordinal));
    }

    public static async Task<StudentRecord> SaveLessonCompletionAsync(
        string studentName,
        string? studentId,
        string sessionId,
        IEnumerable<LessonItemType> completedItems,
        DateTimeOffset completedAt)
    {
        var trimmedName = studentName.Trim();
        var students = await LoadAllAsync();

        var student = students.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(studentId) &&
             string.Equals(x.Id, studentId, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(NormalizeName(x.FullName), NormalizeName(trimmedName), StringComparison.Ordinal));

        if (student is null)
        {
            student = new StudentRecord
            {
                FullName = trimmedName
            };

            students.Add(student);
        }
        else
        {
            student.FullName = trimmedName;
        }

        foreach (var itemType in completedItems.Distinct())
        {
            if (student.CompletedItems.Any(x => x.ItemType == itemType))
                continue;

            student.CompletedItems.Add(new CompletedLessonItem
            {
                ItemType = itemType,
                CompletedAt = completedAt,
                LessonSessionId = sessionId
            });
        }

        student.LastLessonEndedAt = completedAt;

        await SaveAllAsync(students);
        return student;
    }

    private static async Task<List<StudentRecord>> LoadAllAsync()
    {
        if (!File.Exists(StudentsPath))
            return new List<StudentRecord>();

        try
        {
            var json = await File.ReadAllTextAsync(StudentsPath);
            return JsonSerializer.Deserialize<List<StudentRecord>>(json, Options) ?? new List<StudentRecord>();
        }
        catch
        {
            return new List<StudentRecord>();
        }
    }

    private static async Task SaveAllAsync(List<StudentRecord> students)
    {
        var json = JsonSerializer.Serialize(students, Options);
        await File.WriteAllTextAsync(StudentsPath, json);
    }

    private static string NormalizeName(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
