using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper.ValueObject;

public static class StreetAddressMapper
{
        extension(StreetAddressDto dto)
        {
            public StreetAddress ToModel()
            {
                return new StreetAddress(
                    dto.PostalCode,
                    dto.City,
                    dto.Region,
                    dto.AddressLine);
            }
        }
    
        extension(StreetAddress model)
        {
            public StreetAddressDto ToDto()
            {
                return new StreetAddressDto(
                        model.PostalCode,
                        model.City,
                        model.Region,
                        model.AddressLine);
            }
        }
}
