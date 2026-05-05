namespace DrivingSchoolApp.DTOs.ValueObject;

public record PackageDto(
    string Title,
    string Description,
    MoneyDto Price);
