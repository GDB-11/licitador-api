using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Encryption.Response;
using BindSharp;

namespace Application.Core.Interfaces.Shared;

public interface IAsymmetricFieldEncryption
{
    Task<Result<PublicKeyResponse, EncryptionError>> GenerateNewKeyPairAsync();
    Task<Result<T, EncryptionError>> DecryptRequestAsync<T>(Guid keyPairId, T encryptedRequest) where T : class;
}