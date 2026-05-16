using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

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
                    .Select((point, index) => point.ToDto(index+1)).ToArray()
            );
        }

        public DrivingRouteDto ToDto(IReadOnlyList<RouteCoordinate> routeGeometry)
        {
            return new DrivingRouteDto(
                new DateTimeRangeDto(model.StartedAt.UtcDateTime, model.EndedAt.UtcDateTime),
                routeGeometry
                    .Select((coordinate, index) => new CoordinatePointDto(
                        index + 1,
                        (float)coordinate.Latitude,
                        (float)coordinate.Longitude))
                    .ToArray()
            );
        }
    }
}
