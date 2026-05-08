using DrivingSchoolApp.DTOs.Student;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IStudentInviteService
{
    Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId);
    Task<RestResponse<List<StudentInviteDto>>> GetInvitesFromSchoolAsync(Guid schoolId, bool checkCache = true);
    Task<RestResponse> DeleteInviteAsync(Guid schoolId, Guid inviteId);
}