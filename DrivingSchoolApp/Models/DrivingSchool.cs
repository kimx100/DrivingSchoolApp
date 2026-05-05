namespace DrivingSchoolApp.Models;

public sealed record DrivingSchool(
    Guid Id,
    string Name,
    StreetAddress Address,
    string PhoneNumber,
    string WebAddress,
    IReadOnlyList<Package> Packages,
    IReadOnlyList<Student>? Students = null,
    IReadOnlyList<Instructor>? Instructors = null);
