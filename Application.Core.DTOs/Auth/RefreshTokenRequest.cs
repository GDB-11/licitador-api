namespace Application.Core.DTOs.Auth;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}