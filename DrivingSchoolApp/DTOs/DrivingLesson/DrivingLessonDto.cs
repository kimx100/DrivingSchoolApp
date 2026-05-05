using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.DrivingLesson;

public record DrivingLessonDto(
    Guid Id,
    Guid SchoolId,
    Guid? InstructorId,
    Guid? StudentId,
    DrivingRouteDto Route,
    MoneyDto Price,
    DrivingLessonObjectiveDto CompletedObjectives
    );
    