using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper.ValueObject;

public static class CoordinatePointMapper
{
    extension(CoordinatePointDto dto)
    {
        public TrackPoint ToModel()
        {
            return new TrackPoint(
                DateTimeOffset.UtcNow,
                dto.Latitude,
                dto.Longitude
            ); 
        }
    }

    extension(TrackPoint domain)
    {
        public CoordinatePointDto ToDto(int order)
        {
            return new CoordinatePointDto(
                order,
                (float)domain.Latitude,
                (float)domain.Longitude
            );
        }
    }
}
