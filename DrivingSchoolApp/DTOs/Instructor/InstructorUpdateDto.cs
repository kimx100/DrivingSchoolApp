using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Instructor;

public record InstructorUpdateDto(
    NameDto Name,
    string Email,
    string PhoneNumber);