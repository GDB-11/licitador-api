using Application.Core.Config;
using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Global.Objects.Auth;
using Global.Objects.Encryption;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;
using NSubstitute;

namespace Application.Core.Services.Test.Auth;

public sealed class AuthenticationServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPassword _passwordService;
    private readonly IJwt _jwtService;
    private readonly ITimeProvider _timeProvider;
    private readonly IAuthentication _sut;
    private readonly JwtConfig _jwtConfig;
    private readonly DateTime _fixedUtcNow;

    public AuthenticationServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordService = Substitute.For<IPassword>();
        _jwtService = Substitute.For<IJwt>();
        _timeProvider = Substitute.For<ITimeProvider>();

        _fixedUtcNow = new DateTime(2025, 10, 23, 10, 0, 0, DateTimeKind.Utc);
        _timeProvider.UtcNow.Returns(_fixedUtcNow);

        _jwtConfig = new JwtConfig
        {
            SecretKey = "test-secret-key-minimum-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiryMinutes = 30,
            RefreshTokenExpiryMinutes = 10080 // 7 days
        };

        _sut = new AuthenticationService(
            _userRepository,
            _passwordService,
            _jwtService,
            _timeProvider,
            _jwtConfig
        );
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithLoginResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var user = CreateTestUser(email: request.Email);
        var tokens = ("access_token", "refresh_token", _fixedUtcNow.AddMinutes(30));

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(user));
        _passwordService.VerifyPassword(request.Password, user.PasswordHash)
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Success(tokens));
        _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            tokens.Item2,
            _fixedUtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes))
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tokens.Item1, result.Value.AccessToken);
        Assert.Equal(tokens.Item2, result.Value.RefreshToken);
        Assert.Equal(tokens.Item3, result.Value.ExpiresAt);
        Assert.Equal(user.UserId, result.Value.User.UserId);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(user.FullName, result.Value.User.FullName);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
        _passwordService.Received(1).VerifyPassword(request.Password, user.PasswordHash);
        _jwtService.Received(1).GenerateTokens(user);
        await _userRepository.Received(1).UpdateRefreshTokenAsync(
            user.UserId,
            tokens.Item2,
            _fixedUtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest { Email = "nonexistent@example.com", Password = "password123" };

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success((User?)null));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidCredentialsError>(result.Error);
        Assert.Equal("Invalid email or password", result.Error.Message);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
        _passwordService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WhenRepositoryFails_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var repositoryError = new GenericError("Database error");

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Failure(repositoryError));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidCredentialsError>(result.Error);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsUserInactiveError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var inactiveUser = CreateTestUser(email: request.Email, isActive: false);

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(inactiveUser));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<UserInactiveError>(result.Error);
        Assert.Equal("User account is inactive", result.Error.Message);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
        _passwordService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };
        var user = CreateTestUser(email: request.Email);

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(user));
        _passwordService.VerifyPassword(request.Password, user.PasswordHash)
            .Returns(Result<bool, ChaChaEncryptionError>.Success(false));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidCredentialsError>(result.Error);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
        _passwordService.Received(1).VerifyPassword(request.Password, user.PasswordHash);
        _jwtService.DidNotReceive().GenerateTokens(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordVerificationFails_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var user = CreateTestUser(email: request.Email);
        var encryptionError = new ChaChaDecryptError("Decryption failed");

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(user));
        _passwordService.VerifyPassword(request.Password, user.PasswordHash)
            .Returns(Result<bool, ChaChaEncryptionError>.Failure(encryptionError));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidCredentialsError>(result.Error);

        await _userRepository.Received(1).GetByEmailAsync(request.Email);
        _passwordService.Received(1).VerifyPassword(request.Password, user.PasswordHash);
    }

    [Fact]
    public async Task LoginAsync_WhenJwtGenerationFails_ReturnsJwtGenerationError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var user = CreateTestUser(email: request.Email);
        var jwtError = new JwtGenerationError("Token generation failed");

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(user));
        _passwordService.VerifyPassword(request.Password, user.PasswordHash)
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Failure(jwtError));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<JwtGenerationError>(result.Error);
        Assert.Equal("Token generation failed", result.Error.Details);

        _jwtService.Received(1).GenerateTokens(user);
    }

    [Fact]
    public async Task LoginAsync_WhenRefreshTokenStorageFails_ReturnsJwtGenerationError()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var user = CreateTestUser(email: request.Email);
        var tokens = ("access_token", "refresh_token", _fixedUtcNow.AddMinutes(30));
        var storageError = new GenericError("Database error");

        _userRepository.GetByEmailAsync(request.Email)
            .Returns(Result<User?, GenericError>.Success(user));
        _passwordService.VerifyPassword(request.Password, user.PasswordHash)
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Success(tokens));
        _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            tokens.Item2,
            Arg.Any<DateTime>())
            .Returns(Result<Unit, GenericError>.Failure(storageError));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<JwtGenerationError>(result.Error);
        Assert.Equal("Failed to store refresh token", result.Error.Details);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidRefreshToken_ReturnsSuccessWithNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_refresh_token" };
        var user = CreateTestUser();
        var newTokens = ("new_access_token", "new_refresh_token", _fixedUtcNow.AddMinutes(30));

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Success(newTokens));
        _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            newTokens.Item2,
            _fixedUtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes))
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newTokens.Item1, result.Value.AccessToken);
        Assert.Equal(newTokens.Item2, result.Value.RefreshToken);
        Assert.Equal(newTokens.Item3, result.Value.ExpiresAt);
        Assert.Equal(user.UserId, result.Value.User.UserId);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(user.FullName, result.Value.User.FullName);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        _jwtService.Received(1).GenerateTokens(user);
        await _userRepository.Received(1).UpdateRefreshTokenAsync(
            user.UserId,
            newTokens.Item2,
            _fixedUtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRefreshToken_ReturnsInvalidRefreshTokenError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid_token" };

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success((User?)null));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidRefreshTokenError>(result.Error);
        Assert.Equal("Invalid or expired refresh token", result.Error.Message);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        _jwtService.DidNotReceive().GenerateTokens(Arg.Any<User>());
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRepositoryFails_ReturnsInvalidRefreshTokenError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "some_token" };
        var repositoryError = new GenericError("Database error");

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Failure(repositoryError));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidRefreshTokenError>(result.Error);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInactiveUser_ReturnsUserInactiveError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_token" };
        var inactiveUser = CreateTestUser(isActive: false);

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(inactiveUser));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<UserInactiveError>(result.Error);
        Assert.Equal("User account is inactive", result.Error.Message);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        _jwtService.DidNotReceive().GenerateTokens(Arg.Any<User>());
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenJwtGenerationFails_ReturnsJwtGenerationError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_token" };
        var user = CreateTestUser();
        var jwtError = new JwtGenerationError("Token generation failed");

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Failure(jwtError));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<JwtGenerationError>(result.Error);
        Assert.Equal("Token generation failed", result.Error.Details);

        _jwtService.Received(1).GenerateTokens(user);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenStorageFails_ReturnsJwtGenerationError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_token" };
        var user = CreateTestUser();
        var newTokens = ("new_access_token", "new_refresh_token", _fixedUtcNow.AddMinutes(30));
        var storageError = new GenericError("Database error");

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Success(newTokens));
        _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            newTokens.Item2,
            Arg.Any<DateTime>())
            .Returns(Result<Unit, GenericError>.Failure(storageError));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<JwtGenerationError>(result.Error);
        Assert.Equal("Failed to store refresh token", result.Error.Details);
    }

    [Fact]
    public async Task RefreshTokenAsync_StoresRefreshTokenWithCorrectExpiration()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid_token" };
        var user = CreateTestUser();
        var newTokens = ("new_access_token", "new_refresh_token", _fixedUtcNow.AddMinutes(30));
        var expectedExpirationDate = _fixedUtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes);

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _jwtService.GenerateTokens(user)
            .Returns(Result<(string, string, DateTime), JwtGenerationError>.Success(newTokens));
        _userRepository.UpdateRefreshTokenAsync(
            user.UserId,
            newTokens.Item2,
            expectedExpirationDate)
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        Assert.True(result.IsSuccess);

        await _userRepository.Received(1).UpdateRefreshTokenAsync(
            user.UserId,
            newTokens.Item2,
            expectedExpirationDate);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_ReturnsSuccess()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "valid_refresh_token" };
        var user = CreateTestUser();

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _userRepository.ClearRefreshTokenAsync(user.UserId)
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        await _userRepository.Received(1).ClearRefreshTokenAsync(user.UserId);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidRefreshToken_ReturnsInvalidRefreshTokenError()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "invalid_token" };

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success((User?)null));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidRefreshTokenError>(result.Error);
        Assert.Equal("Invalid or expired refresh token", result.Error.Message);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        await _userRepository.DidNotReceive().ClearRefreshTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task LogoutAsync_WithExpiredRefreshToken_ReturnsInvalidRefreshTokenError()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "expired_token" };

        // GetByRefreshTokenAsync already checks expiration in the WHERE clause
        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success((User?)null));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidRefreshTokenError>(result.Error);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        await _userRepository.DidNotReceive().ClearRefreshTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task LogoutAsync_WhenRepositoryGetFails_ReturnsInvalidRefreshTokenError()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "some_token" };
        var repositoryError = new GenericError("Database error");

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Failure(repositoryError));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidRefreshTokenError>(result.Error);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        await _userRepository.DidNotReceive().ClearRefreshTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task LogoutAsync_WhenClearRefreshTokenFails_ReturnsJwtGenerationError()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "valid_token" };
        var user = CreateTestUser();
        var clearError = new GenericError("Database error", "Failed to update user");

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _userRepository.ClearRefreshTokenAsync(user.UserId)
            .Returns(Result<Unit, GenericError>.Failure(clearError));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<JwtGenerationError>(result.Error);
        Assert.Equal("Failed to clear refresh token", result.Error.Details);
        Assert.Equal("Failed to generate JWT token", result.Error.Message);

        await _userRepository.Received(1).GetByRefreshTokenAsync(request.RefreshToken);
        await _userRepository.Received(1).ClearRefreshTokenAsync(user.UserId);
    }

    [Fact]
    public async Task LogoutAsync_ClearsRefreshTokenForCorrectUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { RefreshToken = "user_specific_token" };
        var user = CreateTestUser(userId: userId);

        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .Returns(Result<User?, GenericError>.Success(user));
        _userRepository.ClearRefreshTokenAsync(userId)
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        Assert.True(result.IsSuccess);

        await _userRepository.Received(1).ClearRefreshTokenAsync(userId);
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(
        Guid? userId = null,
        string email = "test@example.com",
        string fullName = "Test User",
        bool isActive = true)
    {
        return new User
        {
            UserId = userId ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = "hashed_password",
            FullName = fullName,
            IsActive = isActive,
            CreatedDate = DateTime.UtcNow,
            RefreshToken = "existing_refresh_token",
            RefreshTokenExpirationDate = DateTime.UtcNow.AddDays(7)
        };
    }

    #endregion
}