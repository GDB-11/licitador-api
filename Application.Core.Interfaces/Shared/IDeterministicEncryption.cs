using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Shared;

public interface IDeterministicEncryption
{
    Result<string, DeterministicEncryptionError> Encrypt(string plaintext);
    Result<string, DeterministicEncryptionError> Decrypt(string ciphertext);
}