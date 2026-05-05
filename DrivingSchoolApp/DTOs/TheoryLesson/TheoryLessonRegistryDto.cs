using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.TheoryLesson;

public record TheoryLessonRegistryDto(
    DateTime LessonDateTime,
    MoneyDto Price,
    Guid StudentId,
    byte[] InstructorSignature,
    byte[] StudentSignature
    );
    