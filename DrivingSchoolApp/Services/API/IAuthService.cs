using DrivingSchoolApp.DTOs.Admin;
using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IAuthService
{
    Task<bool> LoginAdminAsync(LoginDto loginDto);
    Task<bool> LoginInstructorAsync(LoginDto loginDto);
    Task<bool> LoginStudentAsync(LoginDto loginDto);
    Task<bool> HasSavedAccessTokenAsync();
    Task LogoutAsync();

    Task<RestResponse<T>> GetSelfAsync<T>(bool checkCache = true) where T : IUserDto;
}
