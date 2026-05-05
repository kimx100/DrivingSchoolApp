namespace DrivingSchoolApp.Models;

public sealed record Instructor(
    Guid Id,
    Guid SchoolId,
    Name Name,
    string EmailAddress,
    string PhoneNumber,
    List<Guid>? TheoryLessonIDs,
    List<Guid>? DrivingLessonIds);
    