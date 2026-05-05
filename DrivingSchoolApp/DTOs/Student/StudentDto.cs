using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.Student;

public sealed record StudentDto(
    Guid Id, 
    Guid SchoolId, 
    NameDto StudentName, 
    string EmailAddress, 
    string PhoneNumber, 
    List<TheoryLessonDto>? TheoryLessons = null, 
    List<DrivingLessonDto>? DrivingLessons = null);
 