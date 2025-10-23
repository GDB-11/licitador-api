using Application.Core.DTOs.Auth;
using Global.Objects.Auth;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Auth;

public interface IAuthentication
{
    Task<Result<LoginResponse, AuthError>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse, AuthError>> RefreshTokenAsync(RefreshTokenRequest request);
}