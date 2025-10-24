namespace Application.Core.DTOs.Auth;

public sealed record LogoutRequest
{
    public required string RefreshToken { get; init; }
}