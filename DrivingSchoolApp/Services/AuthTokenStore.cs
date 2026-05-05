namespace DrivingSchoolApp.Services;

public sealed class AuthTokenStore
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public Task<string?> GetAccessTokenAsync()
    {
        return SecureStorage.Default.GetAsync(AccessTokenKey);
    }

    public Task<string?> GetRefreshTokenAsync()
    {
        return SecureStorage.Default.GetAsync(RefreshTokenKey);
    }

    public async Task SaveTokensAsync(string accessToken, string? refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
            SecureStorage.Default.Remove(RefreshTokenKey);
        else
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task ClearAsync()
    {
        ClearTokens();
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}
