namespace DrivingSchoolApp.DTOs.CompletedCourse;

public sealed record CompletedCourseRegistryDto(
    DateTime IncludeLessonsFrom,
    string Reason);