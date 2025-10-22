using System.Security.Cryptography;
using System.Text;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Global.Helpers.Encryption;

public static class RsaHelpers
{
    public static Result<(string PublicKey, string PrivateKey), EncryptionError> GenerateKeyPair(int keySize = 2048)
    {
        return ResultExtensions.Try(() =>
            {
                using RSA rsa = RSA.Create(keySize);
                string publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                string privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                return (publicKey, privateKey);
            }, "Failed to generate RSA key pair")
            .MapError(EncryptionError (error) => new KeyGenerationError(error));
    }

    public static Result<RSA, EncryptionError> ImportPrivateKey(string privateKeyBase64)
    {
        return ResultExtensions.Try(() =>
            {
                RSA rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
                return rsa;
            }, "Failed to import private key")
            .MapError(EncryptionError (error) => new DecryptionError(error));
    }

    public static Result<string, EncryptionError> DecryptValue(
        RSA rsa, 
        string encryptedBase64,
        string fieldName)
    {
        return ResultExtensions.Try(() =>
            {
                byte[] decryptedBytes = rsa.Decrypt(
                    Convert.FromBase64String(encryptedBase64),
                    RSAEncryptionPadding.OaepSHA256
                );
                return Encoding.UTF8.GetString(decryptedBytes);
            }, $"Failed to decrypt value")
            .MapError(EncryptionError (error) => new DecryptionError(error, fieldName));
    }
}