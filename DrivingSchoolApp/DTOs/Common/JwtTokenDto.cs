namespace DrivingSchoolApp.DTOs.Common;

/// <summary>
/// Object containing JWT access and refresh tokens
/// </summary>
public record JwtTokenDto
{
    /// <summary>
    /// The JWT access token
    /// </summary>
    public string? AccessToken { get; init; }
    
    /// <summary>
    /// The JWT refresh token
    /// </summary>
    public string? RefreshToken { get; init; }
}