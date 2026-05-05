using System.Text;
using System.Text.Json;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using RestSharp;

namespace DrivingSchoolApp.Services;

public class TheoryLessonStudentService
{
    private readonly RestClient _client = new(ApiConfiguration.BaseUrl);
    private readonly AuthTokenStore _tokenStore = new();

    public async Task<InstructorDto> GetCurrentInstructorAsync()
    {
        var accessToken = await GetRequiredAccessTokenAsync();
        var instructorId = GetInstructorIdFromToken(accessToken);
        return await GetInstructorAsync(instructorId, accessToken);
    }

    public async Task<(InstructorDto Instructor, List<StudentDto> Students)> GetCurrentInstructorWithStudentsAsync()
    {
        var accessToken = await GetRequiredAccessTokenAsync();
        var instructorId = GetInstructorIdFromToken(accessToken);
        var instructor = await GetInstructorAsync(instructorId, accessToken);

        if (instructor.SchoolId == Guid.Empty)
            throw new InvalidOperationException("The instructor response did not include a valid school id.");

        var students = await GetStudentsForSchoolAsync(instructor.SchoolId, accessToken);
        return (instructor, students);
    }

    public async Task<List<StudentDto>> GetStudentsForCurrentInstructorAsync()
    {
        var result = await GetCurrentInstructorWithStudentsAsync();
        return result.Students;
    }

    private async Task<string> GetRequiredAccessTokenAsync()
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Please log in first.");

        return accessToken;
    }

    private static Guid GetInstructorIdFromToken(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("The saved access token is not a valid JWT.");

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var document = JsonDocument.Parse(payloadJson);

            if (!document.RootElement.TryGetProperty("sub", out var subClaim))
                throw new InvalidOperationException("The saved access token is missing the instructor id claim.");

            var sub = subClaim.GetString();
            if (!Guid.TryParse(sub, out var instructorId))
                throw new InvalidOperationException("The saved access token has an invalid instructor id claim.");

            return instructorId;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("The saved access token payload could not be decoded.", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("The saved access token payload is not valid base64url.", ex);
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private async Task<InstructorDto> GetInstructorAsync(Guid instructorId, string accessToken)
    {
        var request = new RestRequest($"/Instructor/{instructorId}", Method.Get);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await _client.ExecuteAsync<InstructorDto>(request);

        if (!response.IsSuccessful || response.Data is null)
            throw new InvalidOperationException(BuildRequestError("Failed to load the current instructor.", response));

        return response.Data;
    }

    private async Task<List<StudentDto>> GetStudentsForSchoolAsync(Guid schoolId, string accessToken)
    {
        var request = new RestRequest($"/DrivingSchool/{schoolId}/student", Method.Get);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await _client.ExecuteAsync<List<StudentDto>>(request);

        if (!response.IsSuccessful)
            throw new InvalidOperationException(BuildRequestError("Failed to load students for the driving school.", response));

        return response.Data ?? [];
    }

    private static string BuildRequestError(string fallbackMessage, RestResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            return response.ErrorMessage;

        if (!string.IsNullOrWhiteSpace(response.Content))
            return $"{fallbackMessage} ({(int)response.StatusCode} {response.StatusCode}: {response.Content})";

        return $"{fallbackMessage} ({(int)response.StatusCode} {response.StatusCode})";
    }
}
