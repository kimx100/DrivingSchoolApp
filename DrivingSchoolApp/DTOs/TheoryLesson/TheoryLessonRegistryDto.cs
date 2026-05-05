using DrivingSchoolApp.DTOs.ValueObject;
using Microsoft.AspNetCore.Http;

namespace DrivingSchoolApp.DTOs.TheoryLesson;

public record TheoryLessonRegistryDto(
    DateTime LessonDateTime,
    MoneyDto Price,
    Guid StudentId,
    IFormFile InstructorSignature,
    IFormFile StudentSignature
    );
    