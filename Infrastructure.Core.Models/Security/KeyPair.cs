namespace Infrastructure.Core.Models.Security;

public sealed class KeyPair
{
    public Guid Id { get; init; }
    public required string PublicKey { get; init; }
    public required string PrivateKey { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? UsedAt { get; init; }
}