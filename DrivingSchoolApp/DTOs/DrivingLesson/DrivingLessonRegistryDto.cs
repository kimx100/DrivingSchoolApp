using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.DrivingLesson;

public record DrivingLessonRegistryDto(
    byte[] InstructorSignature,
    byte[] StudentSignature,
    Guid SchoolId,
    Guid StudentId,
    DrivingRouteDto Route,
    MoneyDto Price,
    DrivingLessonObjectiveDto CompletedObjectives
    );
