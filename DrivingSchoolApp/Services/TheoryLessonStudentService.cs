using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using RestSharp;

namespace DrivingSchoolApp.Services;

public class TheoryLessonStudentService
{
    private readonly RestClient _client = new(ApiConfiguration.BaseUrl);
    private readonly AuthTokenStore _tokenStore = new();

    public async Task<List<StudentDto>> GetStudentsForCurrentInstructorAsync()
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Please log in first.");

        var instructor = await GetCurrentInstructorAsync(accessToken);

        if (instructor.SchoolId == Guid.Empty)
            throw new InvalidOperationException("The logged-in instructor does not have a valid school id.");

        return await GetStudentsForSchoolAsync(instructor.SchoolId, accessToken);
    }

    private async Task<InstructorDto> GetCurrentInstructorAsync(string accessToken)
    {
        var request = new RestRequest("/Auth/self", Method.Get);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await _client.ExecuteAsync<InstructorDto>(request);

        if (!response.IsSuccessful || response.Data is null)
        {
            var message = string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? "Failed to load the current instructor."
                : response.ErrorMessage;

            throw new InvalidOperationException(message);
        }

        return response.Data;
    }

    private async Task<List<StudentDto>> GetStudentsForSchoolAsync(Guid schoolId, string accessToken)
    {
        var request = new RestRequest($"/DrivingSchool/{schoolId}/student", Method.Get);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await _client.ExecuteAsync<List<StudentDto>>(request);

        if (!response.IsSuccessful)
        {
            var message = string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? "Failed to load students for the driving school."
                : response.ErrorMessage;

            throw new InvalidOperationException(message);
        }

        return response.Data ?? [];
    }
}
