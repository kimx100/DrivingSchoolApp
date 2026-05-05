using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.DrivingSchool;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IDrivingSchoolService
{
    Task<RestResponse<DrivingSchoolDto>> GetDrivingSchoolByIdAsync(Guid schoolId);
    Task<RestResponse<DrivingSchoolRatingDto>> GetDrivingSchoolRatingAsync(Guid schoolId);
    Task<RestResponse<DrivingSchoolDto>> CreateDrivingSchoolAsync(DrivingSchoolRegistryDto registryDto);
    Task<RestResponse<List<DrivingSchoolDto>>> GetAllDrivingSchoolsAsync();
    Task<RestResponse<List<StudentDto>>> GetAllStudentsFromSchoolAsync(Guid schoolId);
    Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId);
    Task<RestResponse> DeleteDrivingSchoolAsync(Guid schoolId);
    Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsFromSchoolAsync(Guid schoolId);
    Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsFromSchoolAsync(Guid schoolId);
    Task<RestResponse<List<InstructorDto>>> GetAllInstructorsFromSchoolAsync(Guid schoolId);
    Task<RestResponse<DrivingSchoolDto>> UpdateDrivingSchoolAsync(Guid schoolId, DrivingSchoolUpdateDto updateDto);
}