using Application.Core.Interfaces.Shared;
using Application.Core.Services.Shared;
using Global.Objects.Encryption;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Security;
using Infrastructure.Core.Models.Security;
using NSubstitute;

namespace Application.Core.Services.Test;

public sealed class AsymmetricFieldEncryptionServiceTests
{
    private readonly ITimeProvider _timeProvider;
    private readonly IKeyRepository _keyRepository;
    private readonly IAsymmetricFieldEncryption _sut;
    private readonly DateTime _fixedUtcNow;

    public AsymmetricFieldEncryptionServiceTests()
    {
        _timeProvider = Substitute.For<ITimeProvider>();
        _keyRepository = Substitute.For<IKeyRepository>();
        _sut = new AsymmetricFieldEncryptionService(_timeProvider, _keyRepository);
        _fixedUtcNow = new DateTime(2025, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        _timeProvider.UtcNow.Returns(_fixedUtcNow);
    }

    #region GenerateNewKeyPairAsync Tests

    [Fact]
    public async Task GenerateNewKeyPairAsync_WhenSuccessful_ReturnsPublicKeyResponse()
    {
        // Arrange
        _keyRepository.AddAsync(Arg.Any<KeyPair>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.GenerateNewKeyPairAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.KeyPairId);
        Assert.NotEmpty(result.Value.PublicKey);

        await _keyRepository.Received(1).AddAsync(Arg.Is<KeyPair>(kp =>
            kp.Id != Guid.Empty &&
            !string.IsNullOrEmpty(kp.PublicKey) &&
            !string.IsNullOrEmpty(kp.PrivateKey) &&
            kp.IsActive &&
            kp.ExpiresAt == _fixedUtcNow.AddMinutes(30)
        ));
    }

    [Fact]
    public async Task GenerateNewKeyPairAsync_WhenRepositoryFails_ReturnsKeyGenerationError()
    {
        // Arrange
        var repositoryError = new GenericError("Database error", "Connection failed", new Exception("Connection failed"));
        _keyRepository.AddAsync(Arg.Any<KeyPair>())
            .Returns(Result<Unit, GenericError>.Failure(repositoryError));

        // Act
        var result = await _sut.GenerateNewKeyPairAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<KeyGenerationError>(result.Error);
        Assert.Equal("Database error", result.Error.Message);
        Assert.NotNull(result.Error.Exception);
    }

    [Fact]
    public async Task GenerateNewKeyPairAsync_CreatesKeyPairWithCorrectExpiration()
    {
        // Arrange
        _keyRepository.AddAsync(Arg.Any<KeyPair>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        await _sut.GenerateNewKeyPairAsync();

        // Assert
        await _keyRepository.Received(1).AddAsync(Arg.Is<KeyPair>(kp =>
            kp.ExpiresAt == _fixedUtcNow.AddMinutes(30)
        ));
    }

    #endregion

    #region DecryptRequestAsync Tests

    [Fact]
    public async Task DecryptRequestAsync_WhenKeyNotFound_ReturnsKeyNotFoundError()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest { UnencryptedField = "test" };
        var repositoryError = new GenericError("Key not found");

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Failure(repositoryError));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<KeyNotFoundError>(result.Error);
        Assert.Equal("Key not found", result.Error.Message);
    }

    [Fact]
    public async Task DecryptRequestAsync_WhenKeyAlreadyUsed_ReturnsKeyAlreadyUsedError()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest { UnencryptedField = "test" };
        var usedKeyPair = CreateKeyPair(keyPairId, usedAt: DateTime.UtcNow);

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Success(usedKeyPair));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<KeyAlreadyUsedError>(result.Error);
        Assert.Equal("The key has already been used", result.Error.Message);

        await _keyRepository.DidNotReceive().DeactivateKeyAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DecryptRequestAsync_WhenSuccessful_DeactivatesKey()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest { UnencryptedField = "test" };
        var keyPair = CreateKeyPair(keyPairId);

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Success(keyPair));
        _keyRepository.DeactivateKeyAsync(keyPairId)
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _keyRepository.Received(1).DeactivateKeyAsync(keyPairId);
    }

    [Fact]
    public async Task DecryptRequestAsync_WhenDeactivationFails_StillReturnsSuccessfulDecryption()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest { UnencryptedField = "test" };
        var keyPair = CreateKeyPair(keyPairId);

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Success(keyPair));
        _keyRepository.DeactivateKeyAsync(keyPairId)
            .Returns(Result<Unit, GenericError>.Failure(new GenericError("Deactivation failed")));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        // The Tap operation doesn't affect the result, so decryption succeeds
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DecryptRequestAsync_WithUnencryptedFields_CopiesFieldsCorrectly()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest
        {
            UnencryptedField = "test value",
            AnotherUnencryptedField = 123
        };
        var keyPair = CreateKeyPair(keyPairId);

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Success(keyPair));
        _keyRepository.DeactivateKeyAsync(keyPairId)
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test value", result.Value.UnencryptedField);
        Assert.Equal(123, result.Value.AnotherUnencryptedField);
    }

    [Fact]
    public async Task DecryptRequestAsync_WithInvalidPrivateKey_ReturnsDecryptionError()
    {
        // Arrange
        var keyPairId = Guid.NewGuid();
        var request = new TestEncryptedRequest { UnencryptedField = "test" };
        var keyPairWithInvalidKey = new KeyPair
        {
            Id = keyPairId,
            PublicKey = "invalid-key",
            PrivateKey = "invalid-private-key",
            IsActive = true,
            ExpiresAt = _fixedUtcNow.AddMinutes(30)
        };

        _keyRepository.GetByIdAsync(keyPairId)
            .Returns(Result<KeyPair, GenericError>.Success(keyPairWithInvalidKey));

        // Act
        var result = await _sut.DecryptRequestAsync(keyPairId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<DecryptionError>(result.Error);
    }

    #endregion

    #region Helper Methods and Test Classes

    private static KeyPair CreateKeyPair(Guid id, DateTime? usedAt = null)
    {
        // Generate a real RSA key pair for testing
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        return new KeyPair
        {
            Id = id,
            PublicKey = publicKey,
            PrivateKey = privateKey,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            UsedAt = usedAt
        };
    }

    private sealed class TestEncryptedRequest
    {
        public string? UnencryptedField { get; set; }
        public int AnotherUnencryptedField { get; set; }
    }

    #endregion
}