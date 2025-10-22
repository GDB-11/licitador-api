using Application.Core.Config;
using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Global.Objects.Results;
using System.Security.Cryptography;
using System.Text;

namespace Application.Core.Services.Shared;

public sealed class ChaChaEncryptionService : IChaChaEncryption
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _masterKey;

    public ChaChaEncryptionService(EncryptionConfig config)
    {
        _masterKey = Convert.FromBase64String(config.MasterKey);

        if (_masterKey.Length != 32)
        {
            throw new CryptographicException(
                $"Invalid key size. ChaCha20 requires a 256-bit (32 bytes) key, but received {_masterKey.Length} bytes.");
        }
    }

    public Result<string, ChaChaEncryptionError> Encrypt(string plaintext) =>
        ResultExtensions.Try(
            () => Encoding.UTF8.GetBytes(plaintext),
            "Failed to convert plaintext to bytes"
        )
        .MapError(error => new ChaChaEncryptError(error) as ChaChaEncryptionError)
        .Bind(EncryptToBytes)
        .Map(Convert.ToBase64String);

    public Result<string, ChaChaEncryptionError> Encrypt(byte[] byteContent) =>
        EncryptToBytes(byteContent)
            .Map(Convert.ToBase64String);

    public Result<byte[], ChaChaEncryptionError> EncryptToBytes(string plaintext) =>
        ResultExtensions.Try(
            () => Encoding.UTF8.GetBytes(plaintext),
            "Failed to convert plaintext to bytes"
        )
        .MapError(error => new ChaChaEncryptError(error) as ChaChaEncryptionError)
        .Bind(EncryptToBytes);

    public Result<byte[], ChaChaEncryptionError> EncryptToBytes(byte[] byteContent) =>
        ResultExtensions.Try(
            () => PerformEncryption(byteContent),
            "Failed to encrypt data"
        )
        .MapError(error => new ChaChaEncryptError(error) as ChaChaEncryptionError);

    private byte[] PerformEncryption(byte[] byteContent)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[byteContent.Length];
        byte[] tag = new byte[TagSize];

        using ChaCha20Poly1305 chaCha20Poly1305 = new(_masterKey);
        chaCha20Poly1305.Encrypt(nonce, byteContent, ciphertext, tag);

        return CombineEncryptedParts(nonce, tag, ciphertext);
    }

    private static byte[] CombineEncryptedParts(byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return result;
    }

    public Result<string, ChaChaEncryptionError> Decrypt(string ciphertext) =>
        ResultExtensions.Try(
            () => Convert.FromBase64String(ciphertext),
            "Failed to decode base64 ciphertext"
        )
        .MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError)
        .Bind(DecryptToBytes)
        .Bind(bytes => ResultExtensions.Try(
            () => Encoding.UTF8.GetString(bytes),
            "Failed to convert decrypted bytes to string"
        ).MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError));

    public Result<string, ChaChaEncryptionError> Decrypt(byte[] ciphertext) =>
        DecryptToBytes(ciphertext)
            .Bind(bytes => ResultExtensions.Try(
                () => Encoding.UTF8.GetString(bytes),
                "Failed to convert decrypted bytes to string"
            ).MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError));

    public Result<byte[], ChaChaEncryptionError> DecryptToBytes(string ciphertext) =>
        ResultExtensions.Try(
            () => Convert.FromBase64String(ciphertext),
            "Failed to decode base64 ciphertext"
        )
        .MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError)
        .Bind(DecryptToBytes);

    public Result<byte[], ChaChaEncryptionError> DecryptToBytes(byte[] ciphertext) =>
        ValidateCiphertextLength(ciphertext)
            .Bind(ExtractEncryptedParts)
            .Bind(PerformDecryption);

    private static Result<byte[], ChaChaEncryptionError> ValidateCiphertextLength(byte[] ciphertext)
    {
        int minLength = NonceSize + TagSize;
        return ciphertext.Length >= minLength
            ? Result<byte[], ChaChaEncryptionError>.Success(ciphertext)
            : Result<byte[], ChaChaEncryptionError>.Failure(
                new ChaChaDecryptError($"Ciphertext too short. Expected at least {minLength} bytes, got {ciphertext.Length}"));
    }

    private static Result<(byte[] Nonce, byte[] Tag, byte[] EncryptedData), ChaChaEncryptionError> ExtractEncryptedParts(byte[] ciphertext) =>
        ResultExtensions.Try(() =>
        {
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] encryptedData = new byte[ciphertext.Length - NonceSize - TagSize];

            Buffer.BlockCopy(ciphertext, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(ciphertext, NonceSize + TagSize, encryptedData, 0, encryptedData.Length);

            return (nonce, tag, encryptedData);
        }, "Failed to extract encrypted parts")
        .MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError);

    private Result<byte[], ChaChaEncryptionError> PerformDecryption(
        (byte[] Nonce, byte[] Tag, byte[] EncryptedData) parts) =>
        ResultExtensions.Try(() =>
        {
            byte[] plaintext = new byte[parts.EncryptedData.Length];
            using ChaCha20Poly1305 chaCha20Poly1305 = new(_masterKey);
            chaCha20Poly1305.Decrypt(parts.Nonce, parts.EncryptedData, parts.Tag, plaintext);
            return plaintext;
        }, "Decryption failed. Data might be corrupted or tampered with")
        .MapError(error => new ChaChaDecryptError(error) as ChaChaEncryptionError);
}