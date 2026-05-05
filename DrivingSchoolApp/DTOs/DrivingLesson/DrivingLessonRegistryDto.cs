using Microsoft.AspNetCore.Http;
using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.DrivingLesson;

public record DrivingLessonRegistryDto(
    IFormFile InstructorSignature,
    IFormFile StudentSignature,
    Guid SchoolId,
    Guid StudentId,
    DrivingRouteDto Route,
    MoneyDto Price
    );
