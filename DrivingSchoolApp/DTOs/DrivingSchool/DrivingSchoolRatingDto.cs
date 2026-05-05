using DrivingSchoolApp.DTOs.ValueObject;

namespace DrivingSchoolApp.DTOs.DrivingSchool;

public sealed record DrivingSchoolRatingDto(
    float PassRate,
    float FailRate,
    float QuitRate,
    MoneyDto AveragePrice);
