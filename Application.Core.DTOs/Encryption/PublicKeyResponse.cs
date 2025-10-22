namespace Application.Core.DTOs.Encryption;

public sealed record PublicKeyResponse
{
    public required Guid KeyPairId { get; init; }
    public required string PublicKey { get; init; }
}