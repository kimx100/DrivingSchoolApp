using DrivingSchoolApp.DTOs.Student;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class StudentInviteService(IConfiguration config) : ApiService(config["api_base_url"]!), IStudentInviteService
{
    public async Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite", Method.Post);

        return await ExecuteRequestAsync<StudentInviteDto>(request);
    }

    public async Task<RestResponse<List<StudentInviteDto>>> GetInvitesFromSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite");

        return await ExecuteRequestAsync<List<StudentInviteDto>>(request);
    }

    public async Task<RestResponse> DeleteInviteAsync(Guid schoolId, Guid inviteId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite/{inviteId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }
}