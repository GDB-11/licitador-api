using Dapper;
using Global.Helpers.Functional;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;
using System.Data;

namespace Infrastructure.Core.Services.Account;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public Task<Result<User?, GenericError>> GetByEmailAsync(string email) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByEmailAsync(email),
            errorMessage: "An unexpected error occurred while retrieving the user."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<User?, GenericError>> GetByIdAsync(Guid userId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByIdAsync(userId),
            errorMessage: "An unexpected error occurred while retrieving the user."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<User?, GenericError>> GetByRefreshTokenAsync(string refreshToken) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByRefreshTokenAsync(refreshToken),
            errorMessage: "An unexpected error occurred while retrieving the user by refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteUpdateRefreshTokenAsync(userId, refreshToken, expirationDate),
            errorMessage: "An unexpected error occurred while updating the refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> ClearRefreshTokenAsync(Guid userId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteClearRefreshTokenAsync(userId),
            errorMessage: "An unexpected error occurred while clearing the refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    private async Task<User?> ExecuteQueryByEmailAsync(string email)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "Email" = @Email
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { Email = email }
        );
    }

    private async Task<User?> ExecuteQueryByIdAsync(Guid userId)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "UserId" = @UserId
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { UserId = userId }
        );
    }

    private async Task<User?> ExecuteQueryByRefreshTokenAsync(string refreshToken)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "RefreshToken" = @RefreshToken
                AND "RefreshTokenExpirationDate" > @CurrentDate
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { RefreshToken = refreshToken, CurrentDate = DateTime.UtcNow }
        );
    }

    private async Task<Unit> ExecuteUpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate)
    {
        const string sql = """
            UPDATE "Auth"."Users"
            SET "RefreshToken" = @RefreshToken,
                "RefreshTokenExpirationDate" = @ExpirationDate,
                "UpdatedDate" = @UpdatedDate
            WHERE "UserId" = @UserId
            """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                UserId = userId,
                RefreshToken = refreshToken,
                ExpirationDate = expirationDate,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Unit> ExecuteClearRefreshTokenAsync(Guid userId)
    {
        const string sql = """
            UPDATE "Auth"."Users"
            SET "RefreshToken" = NULL,
                "RefreshTokenExpirationDate" = NULL,
                "UpdatedDate" = @UpdatedDate
            WHERE "UserId" = @UserId
            """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                UserId = userId,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }
}