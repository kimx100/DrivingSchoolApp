using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.TheoryLesson;

public record TheoryLessonDto(
    Guid Id,
    Guid SchoolId,
    Guid InstructorId,
    DateTime LessonDateTime,
    MoneyDto Price,
    List<Guid>? StudentIds = null);
    