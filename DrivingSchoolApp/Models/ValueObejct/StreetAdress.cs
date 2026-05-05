namespace DrivingSchoolApp.Models;

public record StreetAddress(
    string PostalCode,
    string City,
    string Region,
    string AddressLine);