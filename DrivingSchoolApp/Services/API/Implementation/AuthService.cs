using DrivingSchoolApp.DTOs.Admin;
using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class AuthService(IConfiguration config) : ApiService(config["api_base_url"]!), IAuthService
{
    private async Task<bool> LoginAsync(LoginDto loginDto, string userType)
    {
        var request = new RestRequest($"/auth/login/{userType}", Method.Post);
        request.AddJsonBody(loginDto);
        
        var response = await ExecuteRequestAsync<JwtTokenDto>(request);

        if (!response.IsSuccessful)
            return false;

        var token = response.Data!;

        await SecureStorage.SetAsync("access_token", token.AccessToken!);
        await SecureStorage.SetAsync("refresh_token", token.RefreshToken!);

        return true;
    }
    
    public async Task<bool> LoginAdminAsync(LoginDto loginDto)
    {
        return await LoginAsync(loginDto, "admin");
    }

    public async Task<bool> LoginInstructorAsync(LoginDto loginDto)
    {
        return await LoginAsync(loginDto, "instructor");
    }

    public async Task<bool> LoginStudentAsync(LoginDto loginDto)
    {
        return await LoginAsync(loginDto, "student");
    }

    public async Task<RestResponse<T>> GetSelfAsync<T>() where T : IUserDto
    {
        var request = new RestRequest("/auth/self");

        return await ExecuteRequestAsync<T>(request);
    }
}