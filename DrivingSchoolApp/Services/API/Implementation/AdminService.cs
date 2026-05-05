using DrivingSchoolApp.DTOs.Admin;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class AdminService(IConfiguration config) : ApiService(config["api_base_url"]!), IAdminService
{
    public async Task<RestResponse<AdminDto>> CreateAdmin(AdminRegistryDto registryDto)
    {
        var request = new RestRequest("/admin", Method.Post);
        request.AddJsonBody(request);

        return await ExecuteRequestAsync<AdminDto>(request);
    }
}