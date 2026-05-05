using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper.ValueObject;

public static class NameMapper
{
        extension(NameDto dto)
        {
            public Name ToModel()
            {
                return new Name(dto.FirstName, dto.LastName);
            }
        }
    
        extension(Name model)
        {
            public NameDto ToDto()
            {
                return new NameDto(model.FirstName, model.LastName);
            }
        }
}
