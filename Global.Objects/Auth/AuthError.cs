namespace Global.Objects.Auth;

public abstract record AuthError(string Message, string? Details = null, Exception? Exception = null);

public sealed record InvalidCredentialsError()
    : AuthError("Invalid email or password");

public sealed record UserInactiveError()
    : AuthError("User account is inactive");

public sealed record JwtGenerationError(string Details, Exception? Exception = null)
    : AuthError("Failed to generate JWT token", Details, Exception);

public sealed record InvalidRefreshTokenError()
    : AuthError("Invalid or expired refresh token");

public sealed record RefreshTokenNotFoundError()
    : AuthError("Refresh token not found");