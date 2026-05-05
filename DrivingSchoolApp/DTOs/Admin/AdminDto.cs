using DrivingSchoolApp.DTOs.Common;

namespace DrivingSchoolApp.DTOs.Admin;

public record AdminDto(Guid Id, string Email) : IUserDto;