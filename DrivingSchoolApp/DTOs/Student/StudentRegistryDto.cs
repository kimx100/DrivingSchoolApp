using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Student;

public sealed record StudentRegistryDto(
    NameDto StudentName,
    string EmailAddress, 
    string PhoneNumber,
    string Password,
    Guid InviteId);
    