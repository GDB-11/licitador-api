using Application.Core.Config;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Infrastructure.Core.Models.Account;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using System.Text;

namespace Application.Core.Services.Test.Auth;

public sealed class JwtServiceTests
{
    private readonly ITimeProvider _timeProvider;
    private readonly IJwt _sut;
    private readonly JwtConfig _jwtConfig;
    private readonly DateTime _fixedUtcNow;

    public JwtServiceTests()
    {
        _timeProvider = Substitute.For<ITimeProvider>();
        _fixedUtcNow = new DateTime(2025, 10, 23, 10, 0, 0, DateTimeKind.Utc);
        _timeProvider.UtcNow.Returns(_fixedUtcNow);

        // Generate a valid 256-bit (32 bytes) key for HMAC-SHA256
        byte[] keyBytes = new byte[32];
        Random.Shared.NextBytes(keyBytes);

        _jwtConfig = new JwtConfig
        {
            SecretKey = Convert.ToBase64String(keyBytes),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiryMinutes = 30,
            RefreshTokenExpiryMinutes = 10080 // 7 days
        };

        _sut = new JwtService(_jwtConfig, _timeProvider);
    }

    #region GenerateTokens Tests

    [Fact]
    public void GenerateTokens_WithValidUser_ReturnsSuccessWithTokens()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
        Assert.Equal(_fixedUtcNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes), result.Value.ExpiresAt);
    }

    [Fact]
    public void GenerateTokens_WithValidUser_GeneratesValidJwtToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);

        // Validate the token structure (should have 3 parts separated by dots)
        var tokenParts = result.Value.AccessToken.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }

    [Fact]
    public void GenerateTokens_WithValidUser_TokenContainsCorrectClaims()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);

        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(result.Value.AccessToken);

        Assert.Equal(user.UserId.ToString(), token.Subject);
        Assert.Equal(user.Email, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.FullName, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateTokens_WithValidUser_TokenHasCorrectIssuerAndAudience()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);

        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(result.Value.AccessToken);

        Assert.Equal(_jwtConfig.Issuer, token.Issuer);
        Assert.Contains(_jwtConfig.Audience, token.Audiences);
    }

    [Fact]
    public void GenerateTokens_WithValidUser_TokenCanBeValidated()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);

        var handler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Disable lifetime validation due to mocked time provider
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidAudience = _jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey))
        };

        var validationResult = handler.ValidateTokenAsync(result.Value.AccessToken, validationParameters).Result;
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void GenerateTokens_CalledTwice_GeneratesDifferentRefreshTokens()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result1 = _sut.GenerateTokens(user);
        var result2 = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value.RefreshToken, result2.Value.RefreshToken);
    }

    [Fact]
    public void GenerateTokens_CalledTwice_GeneratesDifferentAccessTokens()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result1 = _sut.GenerateTokens(user);
        var result2 = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value.AccessToken, result2.Value.AccessToken);
    }

    [Fact]
    public void GenerateTokens_WithDifferentUsers_GeneratesDifferentTokens()
    {
        // Arrange
        var user1 = CreateTestUser(userId: Guid.NewGuid(), email: "user1@test.com");
        var user2 = CreateTestUser(userId: Guid.NewGuid(), email: "user2@test.com");

        // Act
        var result1 = _sut.GenerateTokens(user1);
        var result2 = _sut.GenerateTokens(user2);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value.AccessToken, result2.Value.AccessToken);
    }

    [Fact]
    public void GenerateTokens_WithSpecialCharactersInUserData_HandlesCorrectly()
    {
        // Arrange
        var user = CreateTestUser(
            email: "user+test@example.com",
            fullName: "José García-O'Neill"
        );

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);

        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(result.Value.AccessToken);

        Assert.Equal(user.Email, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.FullName, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
    }

    [Fact]
    public void GenerateTokens_SetsCorrectExpirationTime()
    {
        // Arrange
        var user = CreateTestUser();
        var expectedExpiration = _fixedUtcNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes);

        // Act
        var result = _sut.GenerateTokens(user);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedExpiration, result.Value.ExpiresAt);

        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(result.Value.AccessToken);

        // ValidTo is in UTC
        Assert.Equal(expectedExpiration, token.ValidTo);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        Assert.NotEmpty(refreshToken);
        Assert.True(IsValidBase64(refreshToken));
    }

    [Fact]
    public void GenerateRefreshToken_Has32ByteLength()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_CalledTwice_GeneratesDifferentTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesMultipleUniqueTokens()
    {
        // Arrange
        var tokens = new HashSet<string>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tokens.Add(_sut.GenerateRefreshToken());
        }

        // Assert
        Assert.Equal(100, tokens.Count); // All tokens should be unique
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(
        Guid? userId = null,
        string email = "test@example.com",
        string fullName = "Test User")
    {
        return new User
        {
            UserId = userId ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = "hashed_password",
            FullName = fullName,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }

    private static bool IsValidBase64(string base64String)
    {
        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}