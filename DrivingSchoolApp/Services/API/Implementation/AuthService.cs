using DrivingSchoolApp.DTOs.Admin;
using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class AuthService : ApiService, IAuthService
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    private readonly IMemoryCache _cache;
    
    public AuthService(IConfiguration config, IMemoryCache cache) : base(config["api_base_url"]!)
    {
        _cache = cache;
    }
    
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
        // Check if token exists
        var token = await SecureStorage.GetAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return false;
        
        // Check if token is still valid
        var request = new RestRequest("/auth/self");
        var response = await ExecuteRequestAsync(request);
        return response.IsSuccessful;
    }

    public Task LogoutAsync()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        _cache.Remove("self");
        return Task.CompletedTask;
    }

    public async Task<RestResponse<T>> GetSelfAsync<T>(bool checkCache = true) where T : IUserDto
    {
        return checkCache
            ? await GetSelfCachedAsync<T>()
            : await GetSelfNoCacheAsync<T>();
    }

    private async Task<RestResponse<T>> GetSelfCachedAsync<T>() where T : IUserDto
    {
        // We check the cache first
        if (_cache.TryGetValue("/auth/self", out RestResponse<T>? instructorDtoResponse) && instructorDtoResponse is not null)
            return instructorDtoResponse; // return if found in cache
        
        var request = new RestRequest("/auth/self");
        
        var result = await ExecuteRequestAsync<T>(request);
        
        // Cache entry lifetime
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set("/auth/self", result, cacheEntryOptions); // Set self in the cache
        
        return result;
    }
    
    private async Task<RestResponse<T>> GetSelfNoCacheAsync<T>() where T : IUserDto
    {
        var request = new RestRequest("/auth/self");
        
        var result = await ExecuteRequestAsync<T>(request);
        
        // Cache entry lifetime
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set("/auth/self", result, cacheEntryOptions); // Set self in the cache
        
        return result;
    }
}
