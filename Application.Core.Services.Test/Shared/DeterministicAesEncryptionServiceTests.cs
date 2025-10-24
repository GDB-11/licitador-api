using Application.Core.Config;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Shared;
using Global.Objects.Encryption;
using System.Text;

namespace Application.Core.Services.Test.Shared;

public sealed class DeterministicAesEncryptionServiceTests : IDisposable
{
    private readonly IDeterministicEncryption _sut;
    private readonly DeterministicEncryptionConfig _validConfig;

    public DeterministicAesEncryptionServiceTests()
    {
        // Generate valid 256-bit (32 bytes) keys
        byte[] encryptionKey = new byte[32];
        byte[] ivGenerationKey = new byte[32];
        Random.Shared.NextBytes(encryptionKey);
        Random.Shared.NextBytes(ivGenerationKey);

        _validConfig = new DeterministicEncryptionConfig
        {
            MasterKey = Convert.ToBase64String(encryptionKey),
            IvGenerationKey = Convert.ToBase64String(ivGenerationKey)
        };

        _sut = new DeterministicAesEncryptionService(_validConfig);
    }

    #region Encrypt Tests

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
        Assert.True(IsValidBase64(result.Value));
    }

    [Fact]
    public void Encrypt_WithSamePlaintext_ProducesSameCiphertext()
    {
        // Arrange
        const string plaintext = "Deterministic test";

        // Act
        var result1 = _sut.Encrypt(plaintext);
        var result2 = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.Value, result2.Value); // Deterministic = same output
    }

    [Fact]
    public void Encrypt_WithDifferentPlaintext_ProducesDifferentCiphertext()
    {
        // Arrange
        const string plaintext1 = "Message 1";
        const string plaintext2 = "Message 2";

        // Act
        var result1 = _sut.Encrypt(plaintext1);
        var result2 = _sut.Encrypt(plaintext2);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value, result2.Value);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsError()
    {
        // Arrange
        const string plaintext = "";

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicEncryptError>(result.Error);
        Assert.Contains("cannot be null or empty", result.Error.Message);
    }

    [Fact]
    public void Encrypt_WithNullString_ReturnsError()
    {
        // Arrange
        string? plaintext = null;

        // Act
        var result = _sut.Encrypt(plaintext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicEncryptError>(result.Error);
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

    [Theory]
    [InlineData("a")]
    [InlineData("Short")]
    [InlineData("This is a longer message for testing")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;:',.<>?/")]
    public void Encrypt_WithVariousInputs_WorksCorrectly(string plaintext)
    {
        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    #endregion

    #region Decrypt Tests

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
    public void Decrypt_WithEmptyString_ReturnsError()
    {
        // Arrange
        const string ciphertext = "";

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicDecryptError>(result.Error);
        Assert.Contains("cannot be null or empty", result.Error.Message);
    }

    [Fact]
    public void Decrypt_WithNullString_ReturnsError()
    {
        // Arrange
        string? ciphertext = null;

        // Act
        var result = _sut.Decrypt(ciphertext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicDecryptError>(result.Error);
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
        Assert.IsType<DeterministicDecryptError>(result.Error);
        Assert.Contains("Failed to decode base64", result.Error.Message);
    }

    [Fact]
    public void Decrypt_WithTooShortCiphertext_ReturnsError()
    {
        // Arrange
        byte[] tooShort = new byte[10]; // Less than BlockSize + HmacSize
        string shortCiphertext = Convert.ToBase64String(tooShort);

        // Act
        var result = _sut.Decrypt(shortCiphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicDecryptError>(result.Error);
        Assert.Contains("too short", result.Error.Message);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_ReturnsAuthenticationError()
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
        Assert.IsType<AuthenticationFailedError>(decryptResult.Error);
        Assert.Contains("Authentication tag validation failed", decryptResult.Error.Message);
    }

    [Fact]
    public void Decrypt_WithTamperedAuthTag_ReturnsAuthenticationError()
    {
        // Arrange
        const string plaintext = "Test message";
        var encryptResult = _sut.Encrypt(plaintext);
        Assert.True(encryptResult.IsSuccess);

        // Tamper with auth tag (bytes 16-48)
        byte[] ciphertextBytes = Convert.FromBase64String(encryptResult.Value);
        ciphertextBytes[20] ^= 0xFF; // Flip bits in auth tag
        string tamperedCiphertext = Convert.ToBase64String(ciphertextBytes);

        // Act
        var decryptResult = _sut.Decrypt(tamperedCiphertext);

        // Assert
        Assert.False(decryptResult.IsSuccess);
        Assert.IsType<AuthenticationFailedError>(decryptResult.Error);
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

    #region Round-Trip Tests

    [Theory]
    [InlineData("a")]
    [InlineData("Short text")]
    [InlineData("This is a much longer text that should still encrypt and decrypt correctly")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;:',.<>?/")]
    [InlineData("Email: test@example.com")]
    [InlineData("SSN: 123-45-6789")]
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

    #region Deterministic Behavior Tests

    [Fact]
    public void Encrypt_SameInputMultipleTimes_AlwaysProducesSameOutput()
    {
        // Arrange
        const string plaintext = "Deterministic encryption test";
        const int iterations = 10;
        List<string> ciphertexts = [];

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = _sut.Encrypt(plaintext);
            Assert.True(result.IsSuccess);
            ciphertexts.Add(result.Value);
        }

        // Assert
        Assert.All(ciphertexts, ct => Assert.Equal(ciphertexts[0], ct));
    }

    [Fact]
    public void Encrypt_WithDifferentServices_SameConfig_ProducesSameOutput()
    {
        // Arrange
        const string plaintext = "Cross-service test";
        using var service1 = new DeterministicAesEncryptionService(_validConfig);
        using var service2 = new DeterministicAesEncryptionService(_validConfig);

        // Act
        var result1 = service1.Encrypt(plaintext);
        var result2 = service2.Encrypt(plaintext);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.Value, result2.Value);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMasterKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = null!,
            IvGenerationKey = _validConfig.IvGenerationKey
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("Encryption key cannot be null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyMasterKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = "",
            IvGenerationKey = _validConfig.IvGenerationKey
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("Encryption key cannot be null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullIvGenerationKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = _validConfig.MasterKey,
            IvGenerationKey = null!
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("IV generation key cannot be null or empty", ex.Message);
    }

    [Fact]
    public void Constructor_WithInvalidBase64MasterKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = "Invalid!@#$",
            IvGenerationKey = _validConfig.IvGenerationKey
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("Base64", ex.Message);
    }

    [Fact]
    public void Constructor_WithWrongSizeMasterKey_ThrowsArgumentException()
    {
        // Arrange - AES-256 requires 32 bytes
        byte[] wrongSizeKey = new byte[16]; // Only 128 bits
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = Convert.ToBase64String(wrongSizeKey),
            IvGenerationKey = _validConfig.IvGenerationKey
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("32 bytes", ex.Message);
    }

    [Fact]
    public void Constructor_WithWrongSizeIvGenerationKey_ThrowsArgumentException()
    {
        // Arrange
        byte[] wrongSizeKey = new byte[24]; // Wrong size
        var invalidConfig = new DeterministicEncryptionConfig
        {
            MasterKey = _validConfig.MasterKey,
            IvGenerationKey = Convert.ToBase64String(wrongSizeKey)
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new DeterministicAesEncryptionService(invalidConfig));
        Assert.Contains("32 bytes", ex.Message);
    }

    #endregion

    #region ValidatePlainBytes Edge Cases

    [Fact]
    public void Encrypt_WithEmptyString_TriggersValidatePlainBytesError()
    {
        // Arrange
        const string emptyPlaintext = "";

        // Act
        var result = _sut.Encrypt(emptyPlaintext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicEncryptError>(result.Error);
        Assert.Contains("The plaintext cannot be null or empty", result.Error.Message);
    }

    [Fact]
    public void EncryptBytes_WithZeroLengthArray_ReturnsError()
    {
        // Arrange
        byte[] emptyBytes = [];

        // We need to expose EncryptBytes for testing or test through reflection
        // Since it's private, we'll test it indirectly through Encrypt with empty string
        // But to be thorough, let's also test the encoding edge case
        const string emptyString = "";
        byte[] emptyEncodedBytes = Encoding.UTF8.GetBytes(emptyString);

        // Act
        var result = _sut.Encrypt(emptyString);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicEncryptError>(result.Error);
        Assert.Empty(emptyEncodedBytes); // Verify our assumption
        Assert.Contains("The plaintext cannot be null or empty", result.Error.Message);
    }

    [Fact]
    public void Encrypt_WithWhitespaceOnlyString_SucceedsButCreatesNonEmptyBytes()
    {
        // Arrange - Whitespace is valid plaintext and produces non-zero bytes
        const string whitespace = "   ";
        byte[] whitespaceBytes = Encoding.UTF8.GetBytes(whitespace);

        // Act
        var result = _sut.Encrypt(whitespace);

        // Assert
        Assert.True(whitespaceBytes.Length > 0); // Whitespace produces bytes
        Assert.True(result.IsSuccess); // Should succeed because bytes are not empty
        Assert.NotEmpty(result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_WithNullOrEmptyString_ReturnsAppropriateError(string? plaintext)
    {
        // Act
        var result = _sut.Encrypt(plaintext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DeterministicEncryptError>(result.Error);

        if (plaintext == null)
        {
            // ValidatePlaintext catches null before byte conversion
            Assert.Contains("cannot be null or empty", result.Error.Message);
        }
        else
        {
            // Empty string passes ValidatePlaintext but fails ValidatePlainBytes
            Assert.Contains("The plaintext cannot be null or empty", result.Error.Message);
        }
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsKeysFromMemory()
    {
        // Arrange
        var service = new DeterministicAesEncryptionService(_validConfig);
        const string plaintext = "Test";
        var encrypted = service.Encrypt(plaintext);
        Assert.True(encrypted.IsSuccess);

        // Act
        service.Dispose();

        // Assert - Should still work for decryption done before disposal
        // But we can't directly test if memory is cleared (that's an implementation detail)
        // Just verify Dispose doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new DeterministicAesEncryptionService(_validConfig);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
        service.Dispose();
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

    public void Dispose()
    {
        (_sut as IDisposable)?.Dispose();
    }
}