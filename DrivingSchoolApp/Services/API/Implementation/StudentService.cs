using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.CompletedCourse;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class StudentService(IConfiguration config) : ApiService(config["api_base_url"]!), IStudentService
{
    public async Task<RestResponse<List<StudentDto>>> GetAllStudentsAsync()
    {
        var request = new RestRequest("/student");

        return await ExecuteRequestAsync<List<StudentDto>>(request);
    }

    public async Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}/theoryLesson");

        return await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
    }

    public async Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}/drivingLesson");

        return await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
    }

    public async Task<RestResponse<List<StudentDto>>> CreateStudentAsync(StudentRegistryDto registryDto)
    {
        var request = new RestRequest("/student", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<List<StudentDto>>(request);
    }

    public async Task<RestResponse> DeleteStudentAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<StudentDto>> GetStudentByIdAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}");

        return await ExecuteRequestAsync<StudentDto>(request);
    }

    public async Task<RestResponse<StudentDto>> UpdateStudentAsync(Guid studentId, StudentUpdateDto updateDto)
    {
        var request = new RestRequest($"/student/{studentId}", Method.Put);
        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync<StudentDto>(request);
    }

    public async Task<RestResponse> UpdateStudentPasswordAsync(Guid studentId, UpdatePasswordDto updateDto)
    {
        var request = new RestRequest($"/student/{studentId}/password", Method.Put);
        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<CompletedCourseDto>> GetCompletedCourseByIdAsync(Guid studentId, Guid courseId)
    {
        var request = new RestRequest($"/student/{studentId}/course/{courseId}");

        return await ExecuteRequestAsync<CompletedCourseDto>(request);
    }

    public async Task<RestResponse<List<CompletedCourseDto>>> GetAllCompletedCoursesAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}/course");

        return await ExecuteRequestAsync<List<CompletedCourseDto>>(request);
    }

    public async Task<RestResponse<CompletedCourseDto>> CreateCompletedCourseAsync(Guid studentId, CompletedCourseRegistryDto registryDto)
    {
        var request = new RestRequest($"/student/{studentId}/course", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<CompletedCourseDto>(request);
    }

    public async Task<RestResponse<TimeSlotDto>> AddCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto)
    {
        var request = new RestRequest($"/student/{studentId}/calender", Method.Post);
        request.AddJsonBody(timeSlotDto);

        return await ExecuteRequestAsync<TimeSlotDto>(request);
    }

    public async Task<RestResponse<StudentCalenderDto>> GetCalenderAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}/calender");

        return await ExecuteRequestAsync<StudentCalenderDto>(request);
    }

    public async Task<RestResponse> RemoveCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto)
    {
        var request = new RestRequest($"/student/{studentId}/calender", Method.Delete);
        request.AddJsonBody(timeSlotDto);

        return await ExecuteRequestAsync(request);
    }
}