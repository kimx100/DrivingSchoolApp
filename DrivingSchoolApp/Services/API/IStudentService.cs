using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.CompletedCourse;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IStudentService
{
    Task<RestResponse<List<StudentDto>>> GetAllStudentsAsync(bool checkCache = true);
    Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsAsync(Guid studentId, bool checkCache = true);
    Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsAsync(Guid studentId, bool checkCache = true);
    Task<RestResponse<List<StudentDto>>> CreateStudentAsync(StudentRegistryDto registryDto);
    Task<RestResponse> DeleteStudentAsync(Guid studentId);
    Task<RestResponse<StudentDto>> GetStudentByIdAsync(Guid studentId, bool checkCache = true);
    Task<RestResponse<StudentDto>> UpdateStudentAsync(Guid studentId, StudentUpdateDto updateDto);
    Task<RestResponse> UpdateStudentPasswordAsync(Guid studentId, UpdatePasswordDto updateDto);
    Task<RestResponse<CompletedCourseDto>> GetCompletedCourseByIdAsync(Guid studentId, Guid courseId, bool checkCache = true);
    Task<RestResponse<List<CompletedCourseDto>>> GetAllCompletedCoursesAsync(Guid studentId, bool checkCache = true);
    Task<RestResponse<CompletedCourseDto>> CreateCompletedCourseAsync(Guid studentId,
        CompletedCourseRegistryDto registryDto);
    Task<RestResponse<TimeSlotDto>> AddCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto);
    Task<RestResponse<StudentCalenderDto>> GetCalenderAsync(Guid studentId, bool checkCache = true);
    Task<RestResponse> RemoveCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto);
}