namespace DrivingSchoolApp.Models;

public sealed record Student(
    Guid Id,
    Guid SchoolId,
    Name StudentName,
    string EmailAddress,
    string PhoneNumber);
