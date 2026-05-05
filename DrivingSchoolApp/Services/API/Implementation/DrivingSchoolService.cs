using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.DrivingSchool;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class DrivingSchoolService(IConfiguration config) : ApiService(config["api_base_url"]!), IDrivingSchoolService
{
    public async Task<RestResponse<DrivingSchoolDto>> GetDrivingSchoolByIdAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}");

        return await ExecuteRequestAsync<DrivingSchoolDto>(request);
    }

    public async Task<RestResponse<DrivingSchoolRatingDto>> GetDrivingSchoolRatingAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/rating");

        return await ExecuteRequestAsync<DrivingSchoolRatingDto>(request);
    }

    public async Task<RestResponse<DrivingSchoolDto>> CreateDrivingSchoolAsync(DrivingSchoolRegistryDto registryDto)
    {
        var request = new RestRequest("/drivingSchool", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<DrivingSchoolDto>(request);
    }

    public async Task<RestResponse<List<DrivingSchoolDto>>> GetAllDrivingSchoolsAsync()
    {
        var request = new RestRequest("/drivingSchool");

        return await ExecuteRequestAsync<List<DrivingSchoolDto>>(request);
    }

    public async Task<RestResponse<List<StudentDto>>> GetAllStudentsFromSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student");

        return await ExecuteRequestAsync<List<StudentDto>>(request);
    }

    public async Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite", Method.Post);

        return await ExecuteRequestAsync<StudentInviteDto>(request);
    }

    public async Task<RestResponse> DeleteDrivingSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsFromSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/theoryLesson");

        return await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
    }

    public async Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsFromSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/drivingLesson");

        return await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
    }

    public async Task<RestResponse<List<InstructorDto>>> GetAllInstructorsFromSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/instructor");

        return await ExecuteRequestAsync<List<InstructorDto>>(request);
    }

    public async Task<RestResponse<DrivingSchoolDto>> UpdateDrivingSchoolAsync(Guid schoolId, DrivingSchoolUpdateDto updateDto)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}", Method.Put);
        request.AddJsonBody(updateDto);
        
        return await ExecuteRequestAsync<DrivingSchoolDto>(request);
    }
}