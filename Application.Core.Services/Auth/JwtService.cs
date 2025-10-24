using Application.Core.Config;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Auth;
using Global.Objects.Results;
using Infrastructure.Core.Models.Account;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Core.Services.Auth;

public sealed class JwtService : IJwt
{
    private readonly JwtConfig _jwtConfig;
    private readonly ITimeProvider _timeProvider;

    public JwtService(JwtConfig jwtConfig, ITimeProvider timeProvider)
    {
        _jwtConfig = jwtConfig;
        _timeProvider = timeProvider;
    }

    public Result<(string AccessToken, string RefreshToken, DateTime ExpiresAt), JwtGenerationError> GenerateTokens(User user) =>
        ResultExtensions.Try(
            operation: () => CreateTokens(user),
            errorMessage: "Failed to generate JWT tokens"
        )
        .MapError(error => new JwtGenerationError(error, null));

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private (string AccessToken, string RefreshToken, DateTime ExpiresAt) CreateTokens(User user)
    {
        DateTime expiresAt = _timeProvider.UtcNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes);
        DateTime issuedAt = _timeProvider.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = issuedAt,
            Issuer = _jwtConfig.Issuer,
            Audience = _jwtConfig.Audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var accessToken = handler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken, expiresAt);
    }
}