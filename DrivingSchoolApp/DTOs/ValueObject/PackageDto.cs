namespace DrivingSchoolApp.DTOs.ValueObject;

public class PackageDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required MoneyDto Price { get; init; }
}