using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Instructor;

public sealed record InstructorDto(
    Guid Id,
    Guid SchoolId,
    NameDto Name,
    string EmailAddress,
    string PhoneNumber,
    List<Guid>? TheoryLessonIDs,
    List<Guid>? DrivingLessonIds) : IUserDto;
