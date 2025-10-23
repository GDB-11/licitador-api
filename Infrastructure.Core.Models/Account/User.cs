namespace Infrastructure.Core.Models.Account;

public sealed class User
{
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public required string FullName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? RefreshTokenExpirationDate { get; init; }
}