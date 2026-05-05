using DrivingSchoolApp.DTOs.Admin;
using RestSharp;

namespace DrivingSchoolApp.Services.API;

public interface IAdminService
{
    Task<RestResponse<AdminDto>> CreateAdmin(AdminRegistryDto registryDto);
}