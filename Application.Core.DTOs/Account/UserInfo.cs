namespace Application.Core.DTOs.Account;

public sealed record UserInfo
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
}