using System.Net;
using DrivingSchoolApp.DTOs.Common;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public abstract class ApiService(string baseUrl) : IApiService
{
    private readonly RestClient _client = new(baseUrl);

    private async Task<bool> Refresh()
    {
        var token = await SecureStorage.GetAsync("refresh_token");
        var refreshRequest = new RestRequest("/auth/refresh", Method.Post);
        if (token is not null)
           refreshRequest.AddHeader("Authorization", $"Bearer {token}");
        
        var refreshResponse = await _client.ExecuteAsync<JwtTokenDto>(refreshRequest);

        if (!refreshResponse.IsSuccessful) return false;

        var newTokens = refreshResponse.Data!;

        await SecureStorage.SetAsync("access_token", newTokens.AccessToken!);

        return true;
    }

    protected async Task<RestResponse<T>> ExecuteRequestAsync<T>(RestRequest request) where T : notnull
    {
        var token = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {token}");

        var initialResponse = await _client.ExecuteAsync<T>(request);

        if (initialResponse.IsSuccessful)
            return initialResponse;
        
        // Some other error happened
        if (initialResponse.StatusCode != HttpStatusCode.Unauthorized)
            return initialResponse;

        // Refresh failed
        if (!await Refresh())
            return initialResponse;

        var newToken = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {newToken}");
        return await _client.ExecuteAsync<T>(request);
    }

    protected async Task<RestResponse> ExecuteRequestAsync(RestRequest request)
    {
        var token = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {token}");

        var initialResponse = await _client.ExecuteAsync(request);

        if (initialResponse.IsSuccessful)
            return initialResponse;
        
        // Some other error happened
        if (initialResponse.StatusCode != HttpStatusCode.Unauthorized)
            return initialResponse;

        // Refresh failed
        if (!await Refresh())
            return initialResponse;

        var newToken = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {newToken}");
        return await _client.ExecuteAsync(request);
    }

    protected async Task<Stream?> ExecuteDownloadRequestAsync(RestRequest request)
    {
        var token = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {token}");

        var initialResponse = await _client.DownloadStreamAsync(request);

        if (initialResponse is not null)
            return initialResponse;
        
        // Refresh failed
        if (!await Refresh())
            return initialResponse;

        var newToken = await SecureStorage.GetAsync("access_token");
        request.AddOrUpdateHeader("Authorization", $"Bearer {newToken}");
        return await _client.DownloadStreamAsync(request);
    }
}