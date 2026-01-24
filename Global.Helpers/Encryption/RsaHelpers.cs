using System.Security.Cryptography;
using System.Text;
using BindSharp;

namespace Global.Helpers.Encryption;

public static class RsaHelpers
{
    public static Result<(string PublicKey, string PrivateKey), Exception> GenerateKeyPair(int keySize = 2048) =>
        Result.Try(() =>
        {
            using var rsa = RSA.Create(keySize);
            string publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            string privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            return (publicKey, privateKey);
        });

    public static Result<RSA, Exception> ImportPrivateKey(string privateKeyBase64) =>
        Result.Try(() =>
        {
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
            return rsa;
        });

    public static Result<string, Exception> DecryptValue(
        RSA rsa, 
        string encryptedBase64,
        string fieldName) =>
        Result.Try(() =>
        {
            byte[] decryptedBytes = rsa.Decrypt(
                Convert.FromBase64String(encryptedBase64),
                RSAEncryptionPadding.OaepSHA256
            );
            return Encoding.UTF8.GetString(decryptedBytes);
        });
}