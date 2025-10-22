using System.Security.Cryptography;
using System.Text;
using Application.Core.Config;
using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Global.Objects.Results;

namespace Application.Core.Services.Shared;

/// <summary>
/// Implements deterministic encryption using AES in CBC mode with
/// a key-derived IV, suitable for database lookups and equality comparisons.
/// </summary>
/// <remarks>
/// WARNING: Deterministic encryption is less secure than randomized encryption and
/// should only be used when absolutely necessary, such as for searchable fields.
/// </remarks>
public sealed class DeterministicAesEncryptionService : IDeterministicEncryption, IDisposable
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _ivGenerationKey;
    private bool _disposed;

    private const int BlockSize = 16;
    private const int HmacSize = 32;

    public DeterministicAesEncryptionService(DeterministicEncryptionConfig config)
    {
        var keyResult = ValidateAndLoadKeys(config);

        if (keyResult.IsFailure)
            throw new ArgumentException(keyResult.Error.Message, nameof(config));

        (_encryptionKey, _ivGenerationKey) = keyResult.Value;
    }

    public Result<string, DeterministicEncryptionError> Encrypt(string plaintext) =>
        ValidatePlaintext(plaintext)
            .Bind(text => ResultExtensions.Try(
                () => Encoding.UTF8.GetBytes(text),
                "Failed to convert plaintext to bytes"
            ).MapError(error => new DeterministicEncryptError(error) as DeterministicEncryptionError))
            .Bind(EncryptBytes)
            .Map(Convert.ToBase64String);

    public Result<string, DeterministicEncryptionError> Decrypt(string ciphertext) =>
        ValidateCiphertext(ciphertext)
            .Bind(text => ResultExtensions.Try(
                () => Convert.FromBase64String(text),
                "Failed to decode base64 ciphertext"
            ).MapError(error => new DeterministicDecryptError(error) as DeterministicEncryptionError))
            .Bind(DecryptBytes)
            .Bind(bytes => ResultExtensions.Try(
                () => Encoding.UTF8.GetString(bytes),
                "Failed to convert decrypted bytes to string"
            ).MapError(error => new DeterministicDecryptError(error) as DeterministicEncryptionError));

    private static Result<string, DeterministicEncryptionError> ValidatePlaintext(string plaintext) =>
        !string.IsNullOrEmpty(plaintext)
            ? Result<string, DeterministicEncryptionError>.Success(plaintext)
            : Result<string, DeterministicEncryptionError>.Failure(
                new DeterministicEncryptError("The plaintext cannot be null or empty"));

    private static Result<string, DeterministicEncryptionError> ValidateCiphertext(string ciphertext) =>
        !string.IsNullOrEmpty(ciphertext)
            ? Result<string, DeterministicEncryptionError>.Success(ciphertext)
            : Result<string, DeterministicEncryptionError>.Failure(
                new DeterministicDecryptError("The ciphertext cannot be null or empty"));

    private Result<byte[], DeterministicEncryptionError> EncryptBytes(byte[] plainBytes) =>
        ValidatePlainBytes(plainBytes)
            .Map(GenerateDeterministicIv)
            .Bind(iv => PerformAesEncryption(plainBytes, iv)
                .Map(encrypted => (iv, encrypted)))
            .Map(data => AddAuthenticationTag(data.iv, data.encrypted))
            .Map(data => CombineEncryptedParts(data.iv, data.authTag, data.ciphertext));

    private Result<byte[], DeterministicEncryptionError> DecryptBytes(byte[] encryptedBytes) =>
        ValidateEncryptedBytesLength(encryptedBytes)
            .Bind(ExtractEncryptedParts)
            .Bind(VerifyAuthenticationTag)
            .Bind(parts => PerformAesDecryption(parts.iv, parts.ciphertext));

    private static Result<byte[], DeterministicEncryptionError> ValidatePlainBytes(byte[] plainBytes) =>
        plainBytes is { Length: > 0 }
            ? Result<byte[], DeterministicEncryptionError>.Success(plainBytes)
            : Result<byte[], DeterministicEncryptionError>.Failure(
                new DeterministicEncryptError("Plain bytes cannot be null or empty"));

    private static Result<byte[], DeterministicEncryptionError> ValidateEncryptedBytesLength(byte[] encryptedBytes)
    {
        int minLength = BlockSize + HmacSize;
        return encryptedBytes is { Length: > 0 } && encryptedBytes.Length > minLength
            ? Result<byte[], DeterministicEncryptionError>.Success(encryptedBytes)
            : Result<byte[], DeterministicEncryptionError>.Failure(
                new DeterministicDecryptError($"Encrypted data too short. Expected more than {minLength} bytes"));
    }

    private byte[] GenerateDeterministicIv(byte[] data)
    {
        using HMACSHA256 hmac = new(_ivGenerationKey);
        byte[] hash = hmac.ComputeHash(data);

        byte[] iv = new byte[BlockSize];
        Buffer.BlockCopy(hash, 0, iv, 0, BlockSize);
        return iv;
    }

    private Result<byte[], DeterministicEncryptionError> PerformAesEncryption(byte[] plainBytes, byte[] iv) =>
        ResultExtensions.Try(() =>
        {
            using Aes aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }, "Failed to perform AES encryption")
        .MapError(error => new DeterministicEncryptError(error) as DeterministicEncryptionError);

    private (byte[] iv, byte[] authTag, byte[] ciphertext) AddAuthenticationTag(byte[] iv, byte[] ciphertext)
    {
        byte[] authTag = ComputeHmac(iv, ciphertext);
        return (iv, authTag, ciphertext);
    }

    private static byte[] CombineEncryptedParts(byte[] iv, byte[] authTag, byte[] ciphertext)
    {
        byte[] result = new byte[iv.Length + authTag.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(authTag, 0, result, iv.Length, authTag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length + authTag.Length, ciphertext.Length);
        return result;
    }

    private static Result<(byte[] iv, byte[] authTag, byte[] ciphertext), DeterministicEncryptionError>
        ExtractEncryptedParts(byte[] encryptedBytes) =>
        ResultExtensions.Try(() =>
        {
            byte[] iv = new byte[BlockSize];
            byte[] authTag = new byte[HmacSize];
            int ciphertextLength = encryptedBytes.Length - BlockSize - HmacSize;
            byte[] ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, BlockSize);
            Buffer.BlockCopy(encryptedBytes, BlockSize, authTag, 0, HmacSize);
            Buffer.BlockCopy(encryptedBytes, BlockSize + HmacSize, ciphertext, 0, ciphertextLength);

            return (iv, authTag, ciphertext);
        }, "Failed to extract encrypted parts")
        .MapError(error => new DeterministicDecryptError(error) as DeterministicEncryptionError);

    private Result<(byte[] iv, byte[] ciphertext), DeterministicEncryptionError>
        VerifyAuthenticationTag((byte[] iv, byte[] authTag, byte[] ciphertext) parts)
    {
        byte[] computedAuthTag = ComputeHmac(parts.iv, parts.ciphertext);

        return CryptographicOperations.FixedTimeEquals(parts.authTag, computedAuthTag)
            ? Result<(byte[], byte[]), DeterministicEncryptionError>.Success((parts.iv, parts.ciphertext))
            : Result<(byte[], byte[]), DeterministicEncryptionError>.Failure(
                new AuthenticationFailedError("Authentication tag validation failed. Data may be corrupted or tampered with"));
    }

    private Result<byte[], DeterministicEncryptionError> PerformAesDecryption(byte[] iv, byte[] ciphertext) =>
        ResultExtensions.Try(() =>
        {
            using Aes aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }, "Decryption failed. Data might be corrupted or tampered with")
        .MapError(error => new DeterministicDecryptError(error) as DeterministicEncryptionError);

    private byte[] ComputeHmac(byte[] iv, byte[] ciphertext)
    {
        byte[] combined = new byte[iv.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, iv.Length, ciphertext.Length);

        using HMACSHA256 hmac = new(_encryptionKey);
        return hmac.ComputeHash(combined);
    }

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        ValidateAndLoadKeys(DeterministicEncryptionConfig config) =>
        ValidateKeyStrings(config)
            .Bind(DecodeKeys)
            .Bind(ValidateKeyLengths)
            .Map(CopyKeysToNewArrays);

    private static Result<DeterministicEncryptionConfig, DeterministicEncryptionError>
        ValidateKeyStrings(DeterministicEncryptionConfig config)
    {
        if (string.IsNullOrEmpty(config.MasterKey))
            return Result<DeterministicEncryptionConfig, DeterministicEncryptionError>.Failure(
                new InvalidKeyConfigurationError("Encryption key cannot be null or empty"));

        if (string.IsNullOrEmpty(config.IvGenerationKey))
            return Result<DeterministicEncryptionConfig, DeterministicEncryptionError>.Failure(
                new InvalidKeyConfigurationError("IV generation key cannot be null or empty"));

        return Result<DeterministicEncryptionConfig, DeterministicEncryptionError>.Success(config);
    }

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        DecodeKeys(DeterministicEncryptionConfig config) =>
        ResultExtensions.Try(() =>
        {
            byte[] encryptionKey = Convert.FromBase64String(config.MasterKey);
            byte[] ivGenerationKey = Convert.FromBase64String(config.IvGenerationKey);
            return (encryptionKey, ivGenerationKey);
        }, "Keys must be valid Base64 strings")
        .MapError(error => new InvalidKeyConfigurationError(error) as DeterministicEncryptionError);

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        ValidateKeyLengths((byte[] encryptionKey, byte[] ivGenerationKey) keys)
    {
        if (keys.encryptionKey.Length != 32)
            return Result<(byte[], byte[]), DeterministicEncryptionError>.Failure(
                new InvalidKeyConfigurationError($"Encryption key must be 32 bytes (256 bits), got {keys.encryptionKey.Length} bytes"));

        if (keys.ivGenerationKey.Length != 32)
            return Result<(byte[], byte[]), DeterministicEncryptionError>.Failure(
                new InvalidKeyConfigurationError($"IV generation key must be 32 bytes (256 bits), got {keys.ivGenerationKey.Length} bytes"));

        return Result<(byte[], byte[]), DeterministicEncryptionError>.Success(keys);
    }

    private static (byte[] encryptionKey, byte[] ivGenerationKey)
        CopyKeysToNewArrays((byte[] encryptionKey, byte[] ivGenerationKey) keys)
    {
        byte[] encryptionKeyCopy = new byte[keys.encryptionKey.Length];
        byte[] ivGenerationKeyCopy = new byte[keys.ivGenerationKey.Length];

        Buffer.BlockCopy(keys.encryptionKey, 0, encryptionKeyCopy, 0, keys.encryptionKey.Length);
        Buffer.BlockCopy(keys.ivGenerationKey, 0, ivGenerationKeyCopy, 0, keys.ivGenerationKey.Length);

        return (encryptionKeyCopy, ivGenerationKeyCopy);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Array.Clear(_encryptionKey, 0, _encryptionKey.Length);
            Array.Clear(_ivGenerationKey, 0, _ivGenerationKey.Length);
        }
        _disposed = true;
    }

    ~DeterministicAesEncryptionService()
    {
        Dispose(false);
    }
}