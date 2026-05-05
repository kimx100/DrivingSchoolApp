using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.DrivingSchool;

public record DrivingSchoolUpdateDto(
    string Name,
    StreetAddressDto StreetAddress,
    string PhoneNumber,
    string WebAddress);