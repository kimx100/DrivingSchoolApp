using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.DrivingSchool;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IDrivingSchoolService
{
    Task<RestResponse<DrivingSchoolDto>> GetDrivingSchoolByIdAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<DrivingSchoolRatingDto>> GetDrivingSchoolRatingAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<DrivingSchoolDto>> CreateDrivingSchoolAsync(DrivingSchoolRegistryDto registryDto);
    Task<RestResponse<List<DrivingSchoolDto>>> GetAllDrivingSchoolsAsync(bool checkCache = true);
    Task<RestResponse<List<StudentDto>>> GetAllStudentsFromSchoolAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId);
    Task<RestResponse> DeleteDrivingSchoolAsync(Guid schoolId);
    Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsFromSchoolAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsFromSchoolAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<List<InstructorDto>>> GetAllInstructorsFromSchoolAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse<DrivingSchoolDto>> UpdateDrivingSchoolAsync(Guid schoolId, DrivingSchoolUpdateDto updateDto);
}