using Application.Core.Config;
using Application.Core.DTOs.Account;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
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

    public Task<Result<LoginResponse, AuthenticationError>> LoginAsync(LoginRequest request) =>
        _userRepository.GetByEmailAsync(request.Email)
            .MapErrorAsync(AuthenticationError (error) => new GetByEmailAsyncError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .BindAsync(user => ValidateUserCredentials(user, request.Password));

    public Task<Result<LoginResponse, AuthenticationError>> RefreshTokenAsync(RefreshTokenRequest request) =>
        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .MapErrorAsync(AuthenticationError (error) => new GetByRefreshTokenAsyncError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .BindAsync(ValidateUserActive)
            .BindAsync(GenerateAndStoreNewTokens);

    public Task<Result<Unit, AuthenticationError>> LogoutAsync(LogoutRequest request) =>
        _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            .MapErrorAsync(AuthenticationError (error) => new GetByRefreshTokenAsyncError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .BindAsync(user => _userRepository.ClearRefreshTokenAsync(user.UserId)
                .MapErrorAsync(AuthenticationError (error) => new JwtGenerationError()));

    private Task<Result<LoginResponse, AuthenticationError>> ValidateUserCredentials(User user, string password)
    {
        if (!user.IsActive)
            return Task.FromResult(Result<LoginResponse, AuthenticationError>.Failure(new UserInactiveError()));

        return _passwordService.VerifyPassword(password, user.PasswordHash)
            .MapError(AuthenticationError (encryptionError) => new ChaChaDecryptError(encryptionError.Message, encryptionError.Details, encryptionError.Exception))
            .Ensure(isValid => isValid, new InvalidUserTokenError())
            .BindAsync(_ => GenerateAndStoreNewTokens(user));
    }

    private static Result<User, AuthenticationError> ValidateUserActive(User user) =>
        user.IsActive
            ? user
            : new UserInactiveError();

    private Task<Result<LoginResponse, AuthenticationError>> GenerateAndStoreNewTokens(User user) =>
        _jwtService.GenerateTokens(user)
            .MapError(AuthenticationError (error) => error)
            .BindAsync(tokens => StoreRefreshToken(user.UserId, tokens.RefreshToken)
                .MapErrorAsync(AuthenticationError (error) => new JwtStorageError(error.Details, error.Exception))
                .MapAsync(_ => GenerateLoginResponse(user, tokens)));

    private Task<Result<Unit, AuthenticationError>> StoreRefreshToken(Guid userId, string refreshToken)
    {
        DateTime expirationDate = _timeProvider.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes);
        return _userRepository.UpdateRefreshTokenAsync(userId, refreshToken, expirationDate)
            .MapErrorAsync(AuthenticationError (error) => new StoreRefreshTokenError(error.Message, error.Details, error.Exception));
    }

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