using System.Data;
using Dapper;
using Global.Helpers.Functional;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Security;
using Infrastructure.Core.Models.Security;

namespace Infrastructure.Core.Services.Security;

public sealed class KeyRepository : IKeyRepository
{
    private readonly IDbConnection _connection;

    public KeyRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public Task<Result<Unit, GenericError>> AddAsync(KeyPair keyPair) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteInsertAsync(keyPair),
            errorMessage: "An unexpected error occurred while creating the encryption key."
        )
        .MapErrorAsync(error => new GenericError(error))
        .BindAsync(affectedRows => ValidateAffectedRows(
            affectedRows, 
            "Error inserting encryption key."
        ));

    public Task<Result<Unit, GenericError>> DeactivateKeyAsync(Guid keyId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteDeactivateAsync(keyId),
            errorMessage: "An unexpected error occurred while disabling the encryption key."
        )
        .MapErrorAsync(error => new GenericError(error))
        .BindAsync(affectedRows => ValidateAffectedRows(
            affectedRows,
            "The encryption key does not exist."
        ));

    public Task<Result<KeyPair, GenericError>> GetByIdAsync(Guid keyId) =>
        ResultExtensions.TryAsync(
                operation: () => ExecuteQueryAsync(keyId),
                errorMessage: "An unexpected error occurred while obtaining the encryption key."
            )
            .MapErrorAsync(error => new GenericError(error))
            .EnsureNotNullAsync(
                new GenericError("The encryption key does not exist or is no longer active.")
            );

    private async Task<int> ExecuteInsertAsync(KeyPair keyPair)
    {
        const string sql = """
            INSERT INTO "Security"."KeyPair"
                ("Id", "PublicKey", "PrivateKey", "IsActive", "ExpiresAt")
            VALUES
                (@Id, @PublicKey, @PrivateKey, @IsActive, @ExpiresAt)
            """;

        return await _connection.ExecuteAsync(sql, new
        {
            keyPair.Id,
            keyPair.PublicKey,
            keyPair.PrivateKey,
            keyPair.IsActive,
            keyPair.ExpiresAt
        });
    }

    private async Task<int> ExecuteDeactivateAsync(Guid keyId)
    {
        const string sql = """
            UPDATE "Security"."KeyPair"
            SET "IsActive" = false
                , "UsedAt" = (NOW() AT TIME ZONE 'UTC')
            WHERE "Id" = @Id
            """;

        return await _connection.ExecuteAsync(sql, new { Id = keyId });
    }

    private async Task<KeyPair?> ExecuteQueryAsync(Guid keyId)
    {
        const string sql = """
            SELECT
                "Id", "PublicKey", "PrivateKey", "IsActive",
                "CreatedAt", "ExpiresAt", "UsedAt"
            FROM "Security"."KeyPair"
            WHERE "Id" = @KeyPairId
                AND "IsActive" = true
                AND "ExpiresAt" > (now()::timestamp)
            """;

        return await _connection.QuerySingleOrDefaultAsync<KeyPair>(
            sql, 
            new { KeyPairId = keyId }
        );
    }

    // 🔧 Validación funcional
    private static Result<Unit, GenericError> ValidateAffectedRows(
        int affectedRows, 
        string errorMessage)
    {
        return affectedRows > 0
            ? Result<Unit, GenericError>.Success(Unit.Value)
            : Result<Unit, GenericError>.Failure(new GenericError(errorMessage));
    }
}