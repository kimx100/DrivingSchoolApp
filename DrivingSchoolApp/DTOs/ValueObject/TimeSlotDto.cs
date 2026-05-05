namespace DrivingSchoolApp.DTOs.ValueObject;

public record TimeSlotDto(
    string Description,
    DateTime StartDateTime,
    DateTime EndDateTime);