using DrivingSchoolApp.DTOs.Common;
using RestSharp;

namespace DrivingSchoolApp.Services;

public sealed class AuthService
{
    private readonly RestClient _client = new(ApiConfiguration.BaseUrl);
    private readonly AuthTokenStore _tokenStore = new();

    public async Task LoginInstructorAsync(string email, string password)
    {
        var request = new RestRequest("/Auth/login/instructor", Method.Post);
        request.AddJsonBody(new LoginDto(email, password));

        var response = await _client.ExecuteAsync<JwtTokenDto>(request);

        if (!response.IsSuccessful || response.Data?.AccessToken is not { Length: > 0 } accessToken)
            throw new InvalidOperationException(BuildLoginError(response));

        await _tokenStore.SaveTokensAsync(accessToken, response.Data.RefreshToken);
    }

    private static string BuildLoginError(RestResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            return response.ErrorMessage;

        if (!string.IsNullOrWhiteSpace(response.Content))
            return $"Login failed. {response.Content}";

        return response.StatusCode == 0
            ? "Login failed. Check that the API is running and reachable."
            : $"Login failed. ({(int)response.StatusCode} {response.StatusCode})";
    }
}
