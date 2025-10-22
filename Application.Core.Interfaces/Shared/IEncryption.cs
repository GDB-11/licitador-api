using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Shared;

public interface IEncryption
{
    Result<string, ChaChaEncryptionError> Encrypt(string plaintext);
    Result<string, ChaChaEncryptionError> Decrypt(string ciphertext);
}