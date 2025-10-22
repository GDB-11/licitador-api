using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Services.Shared;

public sealed class EncryptionService : IEncryption
{
    private readonly IChaChaEncryption _chaChaEncryption;

    public EncryptionService(IChaChaEncryption chaChaEncryption)
    {
        _chaChaEncryption = chaChaEncryption;
    }

    public Result<string, ChaChaEncryptionError> Encrypt(string plaintext) =>
        ValidatePlaintext(plaintext)
            .Bind(_chaChaEncryption.Encrypt);

    public Result<string, ChaChaEncryptionError> Decrypt(string ciphertext) =>
        ValidateCiphertext(ciphertext)
            .Bind(_chaChaEncryption.Decrypt);

    private static Result<string, ChaChaEncryptionError> ValidatePlaintext(string plaintext) =>
        !string.IsNullOrEmpty(plaintext)
            ? Result<string, ChaChaEncryptionError>.Success(plaintext)
            : Result<string, ChaChaEncryptionError>.Failure(
                new ChaChaEncryptError("Plaintext cannot be null or empty"));

    private static Result<string, ChaChaEncryptionError> ValidateCiphertext(string ciphertext) =>
        !string.IsNullOrEmpty(ciphertext)
            ? Result<string, ChaChaEncryptionError>.Success(ciphertext)
            : Result<string, ChaChaEncryptionError>.Failure(
                new ChaChaDecryptError("Ciphertext cannot be null or empty"));
}