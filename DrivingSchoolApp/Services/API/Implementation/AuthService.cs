using DrivingSchoolApp.DTOs.Admin;
using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class AuthService(IConfiguration config) : ApiService(config["api_base_url"]!), IAuthService
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    private async Task<bool> LoginAsync(LoginDto loginDto, string userType)
    {
        var request = new RestRequest($"/auth/login/{userType}", Method.Post);
        request.AddJsonBody(loginDto);
        
        var response = await ExecuteRequestAsync<JwtTokenDto>(request);

        if (!response.IsSuccessful)
            return false;

        var token = response.Data!;

        await SecureStorage.SetAsync(AccessTokenKey, token.AccessToken!);
        await SecureStorage.SetAsync(RefreshTokenKey, token.RefreshToken!);

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

    public async Task<bool> HasSavedAccessTokenAsync()
    {
        var token = await SecureStorage.GetAsync(AccessTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public Task LogoutAsync()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }

    public async Task<RestResponse<T>> GetSelfAsync<T>() where T : IUserDto
    {
        var request = new RestRequest("/auth/self");

        return await ExecuteRequestAsync<T>(request);
    }
}
