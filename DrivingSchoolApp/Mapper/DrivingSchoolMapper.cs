using DrivingSchoolApp.DTOs.DrivingSchool;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Mapper.ValueObject;

namespace DrivingSchoolApp.Mapper;

public static class DrivingSchoolMapper
{
    extension(DrivingSchoolDto dto)
    {
        public DrivingSchool ToModel()
        {
            return new DrivingSchool(
                dto.Id,
                dto.Name,
                dto.StreetAddress.ToModel(),
                dto.PhoneNumber,
                dto.WebAddress,
                dto.Packages.Select(x => x.ToModel()).ToArray(),
                dto.Students?.Select(x => x.ToModel()).ToArray()
            );
        }
    }
    
    extension(DrivingSchool model)
    {
        public DrivingSchoolDto ToDto()
        {
            return new DrivingSchoolDto(
                model.Id,
                model.Name,
                model.Address.ToDto(),
                model.PhoneNumber,
                model.WebAddress,
                model.Packages.Select(x => x.ToDto()).ToArray(),
                model.Students?.Select(x => x.ToDto()).ToArray(),
                model.Instructors?.Select(x => x.ToDto()).ToArray()
            );
        }
    }
}
