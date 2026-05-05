namespace DrivingSchoolApp.DTOs.Instructor;

public record InstructorLoginRequestDto(
    string Email,
    string Password);