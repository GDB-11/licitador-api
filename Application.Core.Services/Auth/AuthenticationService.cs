using Application.Core.Config;
using Application.Core.DTOs.Account;
using Application.Core.DTOs.Auth;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Auth;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;

namespace Application.Core.Services.Auth;

public sealed class AuthenticationService : IAuthentication
{
    private readonly IUserRepository _userRepository;
    private readonly IPassword _passwordService;
    private readonly IJwt _jwtService;
    private readonly ITimeProvider _timeProvider;
    private readonly JwtConfig _jwtConfig;

    public AuthenticationService(
        IUserRepository userRepository,
        IPassword passwordService,
        IJwt jwtService,
        ITimeProvider timeProvider,
        JwtConfig jwtConfig)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _timeProvider = timeProvider;
        _jwtConfig = jwtConfig;
    }

    public Task<Result<LoginResponse, AuthError>> LoginAsync(LoginRequest request) =>
        _userRepository.GetByEmailAsync(request.Email)
            .MapErrorAsync(error => (AuthError)new InvalidCredentialsError())
            .EnsureNotNullAsync(new InvalidCredentialsError())
            .BindAsync(user => ValidateUserCredentials(user, request.Password));

    public Task<Result<LoginResponse, AuthError>> RefreshTokenAsync(RefreshTokenRequest request) =>
        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .MapErrorAsync(error => (AuthError)new InvalidRefreshTokenError())
            .EnsureNotNullAsync(new InvalidRefreshTokenError())
            .BindAsync(user => ValidateUserActive(user))
            .BindAsync(user => GenerateAndStoreNewTokens(user));

    private Task<Result<LoginResponse, AuthError>> ValidateUserCredentials(User user, string password)
    {
        if (!user.IsActive)
            return Task.FromResult(Result<LoginResponse, AuthError>.Failure(new UserInactiveError()));

        return _passwordService.VerifyPassword(password, user.PasswordHash)
            .MapError(encryptionError => (AuthError)new InvalidCredentialsError())
            .BindAsync(isValid => isValid
                ? GenerateAndStoreNewTokens(user)
                : Task.FromResult(Result<LoginResponse, AuthError>.Failure(new InvalidCredentialsError())));
    }

    private static Result<User, AuthError> ValidateUserActive(User user) =>
        user.IsActive
            ? Result<User, AuthError>.Success(user)
            : Result<User, AuthError>.Failure(new UserInactiveError());

    private Task<Result<LoginResponse, AuthError>> GenerateAndStoreNewTokens(User user) =>
        _jwtService.GenerateTokens(user)
            .MapError(error => (AuthError)error)
            .BindAsync(tokens => StoreRefreshToken(user.UserId, tokens.RefreshToken)
                .MapErrorAsync(error => (AuthError)new JwtGenerationError("Failed to store refresh token", null))
                .MapAsync(_ => GenerateLoginResponse(user, tokens)));

    private Task<Result<Unit, GenericError>> StoreRefreshToken(Guid userId, string refreshToken)
    {
        DateTime expirationDate = _timeProvider.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes);
        return _userRepository.UpdateRefreshTokenAsync(userId, refreshToken, expirationDate);
    }

    private Result<LoginResponse, AuthError> GenerateLoginResponse(User user) =>
        _jwtService.GenerateTokens(user)
            .Map(tokens => new LoginResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = new UserInfo
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName
                }
            })
            .MapError(error => (AuthError)error);

    private static LoginResponse GenerateLoginResponse(User user, (string AccessToken, string RefreshToken, DateTime ExpiresAt) tokens) =>
       new()
       {
           AccessToken = tokens.AccessToken,
           RefreshToken = tokens.RefreshToken,
           ExpiresAt = tokens.ExpiresAt,
           User = new UserInfo
           {
               UserId = user.UserId,
               Email = user.Email,
               FullName = user.FullName
           }
       };
}