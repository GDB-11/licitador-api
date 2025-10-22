using Application.Core.Config;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Shared;
using Global.Objects.Encryption;
using System.Security.Cryptography;
using System.Text;

namespace Application.Core.Services.Test;

public sealed class ChaChaEncryptionServiceTests
{
    private readonly IChaChaEncryption _sut;
    private readonly string _validMasterKey;

    public ChaChaEncryptionServiceTests()
    {
        // Generate a valid 256-bit (32 bytes) key for ChaCha20
        byte[] keyBytes = new byte[32];
        Random.Shared.NextBytes(keyBytes);
        _validMasterKey = Convert.ToBase64String(keyBytes);

        var config = new EncryptionConfig { MasterKey = _validMasterKey };
        _sut = new ChaChaEncryptionService(config);
    }

    #region Encrypt String Tests

    [Fact]
    public void Encrypt_WithValidPlaintext_ReturnsEncryptedBase64String()
    {
        // Arrange
        const string plaintext = "Hello, World!";

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.NotEqual(plaintext, result.Value);

        // Verify it's valid base64
        Assert.True(IsValidBase64(result.Value));
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEncryptedResult()
    {
        // Arrange
        const string plaintext = "";

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertexts()
    {
        // Arrange
        const string plaintext = "Secret message";

        // Act
        var result1 = _sut.Encrypt(plaintext);
        var result2 = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value, result2.Value); // Different nonces
    }

    [Fact]
    public void Encrypt_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        const string plaintext = "Hello 🌍 世界 مرحبا";

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    #endregion

    #region Encrypt Bytes Tests

    [Fact]
    public void Encrypt_WithByteArray_ReturnsEncryptedBase64String()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Binary data test");

        // Act
        var result = _sut.Encrypt(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.True(IsValidBase64(result.Value));
    }

    [Fact]
    public void EncryptToBytes_WithString_ReturnsEncryptedByteArray()
    {
        // Arrange
        const string plaintext = "Test message";

        // Act
        var result = _sut.EncryptToBytes(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.True(result.Value.Length > plaintext.Length); // Contains nonce + tag + ciphertext
    }

    [Fact]
    public void EncryptToBytes_WithByteArray_ReturnsEncryptedByteArray()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Raw bytes");

        // Act
        var result = _sut.EncryptToBytes(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.True(result.Value.Length > data.Length);
    }

    #endregion

    #region Decrypt String Tests

    [Fact]
    public void Decrypt_WithValidCiphertext_ReturnsOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "Secret message";
        var encryptResult = _sut.Encrypt(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = _sut.Decrypt(encryptResult.Value);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(plaintext, decryptResult.Value);
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_ReturnsError()
    {
        // Arrange
        const string invalidBase64 = "Not valid base64!@#$";

        // Act
        var result = _sut.Decrypt(invalidBase64);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_ReturnsError()
    {
        // Arrange
        const string plaintext = "Original message";
        var encryptResult = _sut.Encrypt(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Tamper with the ciphertext
        byte[] ciphertextBytes = Convert.FromBase64String(encryptResult.Value);
        ciphertextBytes[^1] ^= 0xFF; // Flip bits in last byte
        string tamperedCiphertext = Convert.ToBase64String(ciphertextBytes);

        // Act
        var decryptResult = _sut.Decrypt(tamperedCiphertext);

        // Assert
        Assert.False(decryptResult.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(decryptResult.Error);
    }

    [Fact]
    public void Decrypt_WithTooShortCiphertext_ReturnsError()
    {
        // Arrange
        byte[] tooShort = new byte[10]; // Less than nonce + tag size (12 + 16 = 28)
        string shortCiphertext = Convert.ToBase64String(tooShort);

        // Act
        var result = _sut.Decrypt(shortCiphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Contains("too short", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decrypt_WithUnicodeCharacters_ReturnsOriginalText()
    {
        // Arrange
        const string plaintext = "🚀 Unicode test 世界 مرحبا";
        var encryptResult = _sut.Encrypt(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = _sut.Decrypt(encryptResult.Value);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(plaintext, decryptResult.Value);
    }

    #endregion

    #region Decrypt Bytes Tests

    [Fact]
    public void Decrypt_WithByteArray_ReturnsOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "Byte array test";
        var encryptResult = _sut.EncryptToBytes(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = _sut.Decrypt(encryptResult.Value);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(plaintext, decryptResult.Value);
    }

    [Fact]
    public void DecryptToBytes_WithString_ReturnsOriginalBytes()
    {
        // Arrange
        const string plaintext = "Original data";
        var encryptResult = _sut.Encrypt(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = _sut.DecryptToBytes(encryptResult.Value);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(plaintext);
        Assert.Equal(expectedBytes, decryptResult.Value);
    }

    [Fact]
    public void DecryptToBytes_WithByteArray_ReturnsOriginalBytes()
    {
        // Arrange
        byte[] originalData = Encoding.UTF8.GetBytes("Binary data");
        var encryptResult = _sut.EncryptToBytes(originalData);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = _sut.DecryptToBytes(encryptResult.Value);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(originalData, decryptResult.Value);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Short text")]
    [InlineData("This is a much longer text that should still encrypt and decrypt correctly")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;:',.<>?/")]
    public void EncryptDecrypt_RoundTrip_PreservesOriginalData(string plaintext)
    {
        // Act
        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted.Value);

        // Assert
        Assert.True(encrypted.IsSuccess);
        Assert.True(decrypted.IsSuccess);
        Assert.Equal(plaintext, decrypted.Value);
    }

    [Fact]
    public void EncryptDecrypt_LargeData_WorksCorrectly()
    {
        // Arrange
        string largePlaintext = new('X', 10_000);

        // Act
        var encrypted = _sut.Encrypt(largePlaintext);
        var decrypted = _sut.Decrypt(encrypted.Value);

        // Assert
        Assert.True(encrypted.IsSuccess);
        Assert.True(decrypted.IsSuccess);
        Assert.Equal(largePlaintext, decrypted.Value);
    }

    #endregion

    #region MapError Coverage Tests

    [Fact]
    public void Encrypt_WithNullString_ReturnsChaChaEncryptError()
    {
        // Arrange
        string? nullPlaintext = null;

        // Act
        var result = _sut.Encrypt(nullPlaintext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
        Assert.Contains("Failed to convert plaintext to bytes", result.Error.Message);
    }

    [Fact]
    public void EncryptToBytes_WithNullString_ReturnsChaChaEncryptError()
    {
        // Arrange
        string? nullPlaintext = null;

        // Act
        var result = _sut.EncryptToBytes(nullPlaintext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
        Assert.Contains("Failed to convert plaintext to bytes", result.Error.Message);
    }

    [Fact]
    public void DecryptToBytes_WithInvalidBase64String_ReturnsChaChaDecryptError()
    {
        // Arrange - These characters are not valid in Base64
        const string invalidBase64 = "This is not base64!@#$%^&*()";

        // Act
        var result = _sut.DecryptToBytes(invalidBase64);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Contains("Failed to decode base64 ciphertext", result.Error.Message);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void DecryptToBytes_WithWhitespaceInBase64_ReturnsChaChaDecryptError()
    {
        // Arrange - Base64 with invalid whitespace in the middle
        const string invalidBase64 = "SGVs bG8="; // "Hello" with space in middle

        // Act
        var result = _sut.DecryptToBytes(invalidBase64);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void DecryptToBytes_WithNullString_ReturnsChaChaDecryptError()
    {
        // Arrange
        string? nullCiphertext = null;

        // Act
        var result = _sut.DecryptToBytes(nullCiphertext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Contains("Failed to decode base64 ciphertext", result.Error.Message);
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_TriggersMapErrorBeforeDecryption()
    {
        // Arrange
        const string invalidBase64 = "Not@Valid#Base64$";

        // Act
        var result = _sut.Decrypt(invalidBase64);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        // Verify it fails at the Base64 decode stage, not at decryption
        Assert.Contains("Failed to decode base64 ciphertext", result.Error.Message);
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("???")]
    [InlineData("===")]
    [InlineData("A@B#C$")]
    public void DecryptToBytes_WithVariousInvalidBase64Formats_ReturnsChaChaDecryptError(string invalidBase64)
    {
        // Act
        var result = _sut.DecryptToBytes(invalidBase64);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Contains("Failed to decode base64 ciphertext", result.Error.Message);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithInvalidBase64Key_ThrowsException()
    {
        // Arrange
        var invalidConfig = new EncryptionConfig { MasterKey = "Invalid!@#$" };

        // Act & Assert
        Assert.Throws<FormatException>(() => new ChaChaEncryptionService(invalidConfig));
    }

    [Fact]
    public void Constructor_WithWrongKeySizeKey_ThrowsException()
    {
        // Arrange - ChaCha20 requires 256-bit (32 bytes) key
        byte[] wrongSizeKey = new byte[16]; // Only 128 bits
        var invalidConfig = new EncryptionConfig { MasterKey = Convert.ToBase64String(wrongSizeKey) };

        // Act & Assert
        Assert.Throws<CryptographicException>(() => new ChaChaEncryptionService(invalidConfig));
    }

    #endregion

    #region Helper Methods

    private static bool IsValidBase64(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}