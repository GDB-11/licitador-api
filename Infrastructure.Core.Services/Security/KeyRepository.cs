using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Security;
using Infrastructure.Core.Interfaces.Security;
using Infrastructure.Core.Models.Security;

namespace Infrastructure.Core.Services.Security;

public sealed class KeyRepository : BaseDatabaseService, IKeyRepository
{
    private readonly IDbConnection _connection;

    public KeyRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Result<Unit, SecurityError>> AddAsync(KeyPair keyPair) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, KeyRepositorySql.Insert, new
            {
                keyPair.Id,
                keyPair.PublicKey,
                keyPair.PrivateKey,
                keyPair.IsActive,
                keyPair.ExpiresAt
            }),
            errorFactory: SecurityError (ex) => new AddAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<SecurityError>(
            affectedRows,
            msg => new AddAsyncError(msg),
            "Error inserting encryption key."
        ));

    public async Task<Result<Unit, SecurityError>> DeactivateKeyAsync(Guid keyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, KeyRepositorySql.Deactivate, new { Id = keyId }),
            errorFactory: SecurityError (ex) => new DeactivateKeyAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<SecurityError>(
            affectedRows,
            msg => new DeactivateKeyAsyncError(msg),
            "The encryption key does not exist."
        ));

    public async Task<Result<KeyPair?, SecurityError>> GetByIdAsync(Guid keyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, KeyPair?>(_connection, KeyRepositorySql.GetById, new { KeyPairId = keyId }),
            errorFactory: SecurityError (ex) => new GetByIdAsyncError(ex.Message, ex)
        );
}