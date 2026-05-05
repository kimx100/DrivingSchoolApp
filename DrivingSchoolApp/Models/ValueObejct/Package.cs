namespace DrivingSchoolApp.Models;

public sealed record Package(
    string Title,
    string Description,
    Money Price );