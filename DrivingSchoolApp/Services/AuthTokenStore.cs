namespace DrivingSchoolApp.Services;

public class AuthTokenStore
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

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public void ClearTokens()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
    }
}
