using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Auth;

public interface IPassword
{
    Result<string, ChaChaEncryptionError> HashPassword(string password);
    Result<bool, ChaChaEncryptionError> VerifyPassword(string password, string passwordHash);
}