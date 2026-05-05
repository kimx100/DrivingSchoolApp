using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper.ValueObject;

public static class PackageMapper
{
        extension(PackageDto dto)
        {
            public Package ToModel()
            {
                return new Package(
                    dto.Title,
                    dto.Description,
                    dto.Price.ToModel());
            }
        }
    
        extension(Package model)
        {
            public PackageDto ToDto()
            {
                return new PackageDto(
                    model.Title,
                    model.Description,
                    model.Price.ToDto()
                );
            }
        }
}

