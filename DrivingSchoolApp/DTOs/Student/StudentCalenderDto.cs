using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Student;

public sealed record StudentCalenderDto(
    string StudentId,
    List<TimeSlotDto> TimeSlots);
