using DrivingSchoolApp.DTOs.Student;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class StudentInviteService : ApiService, IStudentInviteService
{
    private readonly IMemoryCache _cache;
    
    public StudentInviteService(IConfiguration config, IMemoryCache cache) : base(config["api_base_url"]!)
    {
        _cache = cache;
    }
    
    public async Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite", Method.Post);

        return await ExecuteRequestAsync<StudentInviteDto>(request);
    }

    public async Task<RestResponse<List<StudentInviteDto>>> GetInvitesFromSchoolAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}/student/invite", out RestResponse<List<StudentInviteDto>>? invites)
            && invites is not null) return invites;

        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite");
        var response = await ExecuteRequestAsync<List<StudentInviteDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/student/invite", response, cacheEntryOptions);
        
        return response;
    }

    public async Task<RestResponse> DeleteInviteAsync(Guid schoolId, Guid inviteId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite/{inviteId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }
}