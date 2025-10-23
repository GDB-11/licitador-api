using Global.Objects.Auth;
using Global.Objects.Results;
using Infrastructure.Core.Models.Account;

namespace Application.Core.Interfaces.Auth;

public interface IJwt
{
    Result<(string AccessToken, string RefreshToken, DateTime ExpiresAt), JwtGenerationError> GenerateTokens(User user);
    string GenerateRefreshToken();
}