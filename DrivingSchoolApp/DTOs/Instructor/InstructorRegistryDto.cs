using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Instructor;

public sealed record InstructorRegistryDto(
    Guid SchoolId,
    NameDto Name,
    string Email,
    string PhoneNumber,
    string Password);
