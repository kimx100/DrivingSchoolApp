namespace DrivingSchoolApp.DTOs.Common;

public record UpdatePasswordDto(
    string OldPassword,
    string NewPassword);