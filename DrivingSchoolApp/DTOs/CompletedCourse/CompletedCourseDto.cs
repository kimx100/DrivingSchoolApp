using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.CompletedCourse;

public sealed record CompletedCourseDto(
    Guid Id,
    Guid SchoolId,
    MoneyDto Cost,
    DateTime CompletionDate,
    string Reason);