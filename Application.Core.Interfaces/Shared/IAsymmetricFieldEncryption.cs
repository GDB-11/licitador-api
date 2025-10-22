using Application.Core.DTOs.Encryption;
using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Shared;

public interface IAsymmetricFieldEncryption
{
    Task<Result<PublicKeyResponse, EncryptionError>> GenerateNewKeyPairAsync();
    Task<Result<T, EncryptionError>> DecryptRequestAsync<T>(Guid keyPairId, T encryptedRequest) where T : class;
}