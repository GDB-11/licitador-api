using Application.Core.DTOs.Auth.Errors;
using BindSharp;
using Infrastructure.Core.Models.Account;

namespace Application.Core.Interfaces.Auth;

public interface IJwt
{
    Result<(string AccessToken, string RefreshToken, DateTime ExpiresAt), AuthenticationError> GenerateTokens(User user);
    string GenerateRefreshToken();
}