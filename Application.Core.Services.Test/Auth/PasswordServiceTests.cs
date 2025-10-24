using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Global.Objects.Encryption;
using Global.Objects.Results;
using NSubstitute;

namespace Application.Core.Services.Test.Auth;

public sealed class PasswordServiceTests
{
    private readonly IEncryption _encryptionService;
    private readonly IPassword _sut;

    public PasswordServiceTests()
    {
        _encryptionService = Substitute.For<IEncryption>();
        _sut = new PasswordService(_encryptionService);
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsSuccessWithHash()
    {
        // Arrange
        const string password = "SecurePassword123!";
        const string expectedHash = "encrypted_hash_value";

        _encryptionService.Encrypt(password)
            .Returns(Result<string, ChaChaEncryptionError>.Success(expectedHash));

        // Act
        var result = _sut.HashPassword(password);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHash, result.Value);

        _encryptionService.Received(1).Encrypt(password);
    }

    [Fact]
    public void HashPassword_WhenEncryptionFails_ReturnsError()
    {
        // Arrange
        const string password = "MyPassword";
        var encryptionError = new ChaChaEncryptError("Encryption failed", new Exception("Crypto error"));

        _encryptionService.Encrypt(password)
            .Returns(Result<string, ChaChaEncryptionError>.Failure(encryptionError));

        // Act
        var result = _sut.HashPassword(password);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaEncryptError>(result.Error);
        Assert.Equal("Encryption failed", result.Error.Message);
        Assert.NotNull(result.Error.Exception);
    }

    [Theory]
    [InlineData("ShortPwd")]
    [InlineData("VeryLongPasswordWithSpecialCharacters!@#$%^&*()_+-=[]{}|;:,.<>?")]
    [InlineData("password with spaces")]
    [InlineData("пароль")] // Unicode characters
    public void HashPassword_WithVariousPasswordFormats_CallsEncryptionService(string password)
    {
        // Arrange
        const string expectedHash = "hashed_value";

        _encryptionService.Encrypt(password)
            .Returns(Result<string, ChaChaEncryptionError>.Success(expectedHash));

        // Act
        var result = _sut.HashPassword(password);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedHash, result.Value);

        _encryptionService.Received(1).Encrypt(password);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_WithMatchingPassword_ReturnsSuccessTrue()
    {
        // Arrange
        const string password = "CorrectPassword";
        const string passwordHash = "encrypted_hash";
        const string decryptedPassword = "CorrectPassword";

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(decryptedPassword));

        // Act
        var result = _sut.VerifyPassword(password, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        _encryptionService.Received(1).Decrypt(passwordHash);
    }

    [Fact]
    public void VerifyPassword_WithNonMatchingPassword_ReturnsSuccessFalse()
    {
        // Arrange
        const string password = "WrongPassword";
        const string passwordHash = "encrypted_hash";
        const string decryptedPassword = "CorrectPassword";

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(decryptedPassword));

        // Act
        var result = _sut.VerifyPassword(password, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _encryptionService.Received(1).Decrypt(passwordHash);
    }

    [Fact]
    public void VerifyPassword_WhenDecryptionFails_ReturnsError()
    {
        // Arrange
        const string password = "MyPassword";
        const string passwordHash = "invalid_hash";
        var decryptionError = new ChaChaDecryptError("Decryption failed", new Exception("Invalid format"));

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Failure(decryptionError));

        // Act
        var result = _sut.VerifyPassword(password, passwordHash);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ChaChaDecryptError>(result.Error);
        Assert.Equal("Decryption failed", result.Error.Message);
        Assert.NotNull(result.Error.Exception);
    }

    [Fact]
    public void VerifyPassword_WithCaseSensitivePassword_ReturnsFalse()
    {
        // Arrange
        const string password = "Password123";
        const string passwordHash = "encrypted_hash";
        const string decryptedPassword = "password123"; // Different case

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(decryptedPassword));

        // Act
        var result = _sut.VerifyPassword(password, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value); // Should be case-sensitive
    }

    [Fact]
    public void VerifyPassword_WithEmptyDecryptedPassword_HandlesCorrectly()
    {
        // Arrange
        const string password = "SomePassword";
        const string passwordHash = "encrypted_hash";
        const string decryptedPassword = "";

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(decryptedPassword));

        // Act
        var result = _sut.VerifyPassword(password, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Theory]
    [InlineData("password", "password", true)]
    [InlineData("password", "Password", false)]
    [InlineData("", "", true)]
    [InlineData("test", "test123", false)]
    [InlineData("пароль", "пароль", true)] // Unicode
    public void VerifyPassword_WithVariousPasswordComparisons_ReturnsExpectedResult(
        string inputPassword,
        string decryptedPassword,
        bool expectedMatch)
    {
        // Arrange
        const string passwordHash = "encrypted_hash";

        _encryptionService.Decrypt(passwordHash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(decryptedPassword));

        // Act
        var result = _sut.VerifyPassword(inputPassword, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedMatch, result.Value);
    }

    #endregion
}