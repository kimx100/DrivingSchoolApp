namespace DrivingSchoolApp.DTOs;

public record StudentLoginRequestDto(
    string Email,
    string Password);