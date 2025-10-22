using Application.Core.Interfaces.Shared;
using Application.Core.Services.Shared;
using Global.Objects.Encryption;
using Global.Objects.Results;
using NSubstitute;

namespace Application.Core.Services.Test;

public sealed class EncryptionServiceTests
{
    private readonly IChaChaEncryption _chaChaEncryption;
    private readonly IEncryption _sut;

    public EncryptionServiceTests()
    {
        _chaChaEncryption = Substitute.For<IChaChaEncryption>();
        _sut = new EncryptionService(_chaChaEncryption);
    }

    #region Encrypt Tests

    [Fact]
    public void Encrypt_WithValidPlaintext_ReturnsSuccessWithEncryptedHash()
    {
        // Arrange
        const string plaintext = "Secret message";
        const string expectedHash = "encrypted_hash_base64";

        _chaChaEncryption.Encrypt(plaintext)
            .Returns(Result<string, ChaChaEncryptionError>.Success(expectedHash));

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHash, result.Value);

        _chaChaEncryption.Received(1).Encrypt(plaintext);
    }

    [Fact]
    public void Encrypt_WithEmptyPlaintext_ReturnsChaChaEncryptError()
    {
        // Arrange
        const string plaintext = "";

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
        Assert.Equal("Plaintext cannot be null or empty", result.Error.Message);

        _chaChaEncryption.DidNotReceive().Encrypt(Arg.Any<string>());
    }

    [Fact]
    public void Encrypt_WithNullPlaintext_ReturnsChaChaEncryptError()
    {
        // Arrange
        string? plaintext = null;

        // Act
        var result = _sut.Encrypt(plaintext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
        Assert.Equal("Plaintext cannot be null or empty", result.Error.Message);

        _chaChaEncryption.DidNotReceive().Encrypt(Arg.Any<string>());
    }

    [Fact]
    public void Encrypt_WhenChaChaEncryptionFails_ReturnsError()
    {
        // Arrange
        const string plaintext = "Test message";
        var encryptionError = new ChaChaEncryptError("Encryption failed", new Exception("Crypto error"));

        _chaChaEncryption.Encrypt(plaintext)
            .Returns(Result<string, ChaChaEncryptionError>.Failure(encryptionError));

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(encryptionError, result.Error);
        Assert.Equal("Encryption failed", result.Error.Message);
        Assert.NotNull(result.Error.Exception);
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("This is a longer message with special chars: !@#$%")]
    [InlineData("Unicode: 🔒 世界")]
    public void Encrypt_WithVariousPlainTexts_CallsChaChaEncryption(string plaintext)
    {
        // Arrange
        _chaChaEncryption.Encrypt(plaintext)
            .Returns(Result<string, ChaChaEncryptionError>.Success("encrypted"));

        // Act
        var result = _sut.Encrypt(plaintext);

        // Assert
        Assert.True(result.IsSuccess);
        _chaChaEncryption.Received(1).Encrypt(plaintext);
    }

    #endregion

    #region Decrypt Tests

    [Fact]
    public void Decrypt_WithValidCiphertext_ReturnsSuccessWithPlainText()
    {
        // Arrange
        const string ciphertext = "encrypted_hash_base64";
        const string expectedPlainText = "Decrypted message";

        _chaChaEncryption.Decrypt(ciphertext)
            .Returns(Result<string, ChaChaEncryptionError>.Success(expectedPlainText));

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPlainText, result.Value);

        _chaChaEncryption.Received(1).Decrypt(ciphertext);
    }

    [Fact]
    public void Decrypt_WithEmptyCiphertext_ReturnsChaChaDecryptError()
    {
        // Arrange
        const string ciphertext = "";

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Equal("Ciphertext cannot be null or empty", result.Error.Message);

        _chaChaEncryption.DidNotReceive().Decrypt(Arg.Any<string>());
    }

    [Fact]
    public void Decrypt_WithNullCiphertext_ReturnsChaChaDecryptError()
    {
        // Arrange
        string? ciphertext = null;

        // Act
        var result = _sut.Decrypt(ciphertext!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Equal("Ciphertext cannot be null or empty", result.Error.Message);

        _chaChaEncryption.DidNotReceive().Decrypt(Arg.Any<string>());
    }

    [Fact]
    public void Decrypt_WhenChaChaDecryptionFails_ReturnsError()
    {
        // Arrange
        const string ciphertext = "invalid_ciphertext";
        var decryptionError = new ChaChaDecryptError("Decryption failed", new Exception("Invalid data"));

        _chaChaEncryption.Decrypt(ciphertext)
            .Returns(Result<string, ChaChaEncryptionError>.Failure(decryptionError));

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(decryptionError, result.Error);
        Assert.Equal("Decryption failed", result.Error.Message);
        Assert.NotNull(result.Error.Exception);
    }

    [Fact]
    public void Decrypt_WithTamperedData_ReturnsError()
    {
        // Arrange
        const string ciphertext = "tampered_base64";
        var error = new ChaChaDecryptError("Authentication failed");

        _chaChaEncryption.Decrypt(ciphertext)
            .Returns(Result<string, ChaChaEncryptionError>.Failure(error));

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("YWJjMTIz")]
    [InlineData("dGVzdA==")]
    public void Decrypt_WithVariousCipherTexts_CallsChaChaDecryption(string ciphertext)
    {
        // Arrange
        _chaChaEncryption.Decrypt(ciphertext)
            .Returns(Result<string, ChaChaEncryptionError>.Success("decrypted"));

        // Act
        var result = _sut.Decrypt(ciphertext);

        // Assert
        Assert.True(result.IsSuccess);
        _chaChaEncryption.Received(1).Decrypt(ciphertext);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Encrypt_ValidationFailure_DoesNotCallChaChaEncryption()
    {
        // Arrange
        const string emptyPlaintext = "";

        // Act
        var result = _sut.Encrypt(emptyPlaintext);

        // Assert
        Assert.False(result.IsSuccess);
        _chaChaEncryption.DidNotReceive().Encrypt(Arg.Any<string>());
    }

    [Fact]
    public void Decrypt_ValidationFailure_DoesNotCallChaChaDecryption()
    {
        // Arrange
        const string emptyCiphertext = "";

        // Act
        var result = _sut.Decrypt(emptyCiphertext);

        // Assert
        Assert.False(result.IsSuccess);
        _chaChaEncryption.DidNotReceive().Decrypt(Arg.Any<string>());
    }

    #endregion

    #region Error Type Tests

    [Fact]
    public void Encrypt_ValidationError_ReturnsChaChaEncryptError()
    {
        // Act
        var result = _sut.Encrypt("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
    }

    [Fact]
    public void Decrypt_ValidationError_ReturnsChaChaDecryptError()
    {
        // Act
        var result = _sut.Decrypt("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
    }

    #endregion
}