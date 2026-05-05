using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Student;

public record StudentUpdateDto(
    NameDto Name,
    string Email,
    string PhoneNumber);