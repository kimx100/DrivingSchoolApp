namespace DrivingSchoolApp.DTOs.ValueObject;

public record DrivingRouteDto(
    DateTimeRangeDto DateTimeRange,
    CoordinatePointDto[] RouteCoordinates);
    