using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper;

public static class DrivingRouteMapper
{
    extension(DrivingRouteDto dto)
    {
        public RouteSession ToModel()
        {
            return new RouteSession
            {
                Id = Guid.NewGuid().ToString("N"),
                StartedAt = dto.DateTimeRange.StartDateTime,
                EndedAt = dto.DateTimeRange.StartDateTime,
                Points = dto.RouteCoordinates.Select(x=>x.ToModel()).ToList()
            };
        }
    }

    extension(RouteSession model)
    {
        public DrivingRouteDto ToDto()
        {
            return new DrivingRouteDto(
                new DateTimeRangeDto(model.StartedAt.UtcDateTime, model.EndedAt.UtcDateTime),
                model.Points.OrderBy(x => x.Timestamp)
                    .Select((point, index) => point.ToDto(index)).ToArray()
            );
        }
    }
}