using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.Company;
using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Account;

namespace Infrastructure.Core.Services.Account;

public sealed class UserRepository : BaseDatabaseService, IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<Result<User?, UserError>> GetByEmailAsync(string email) =>
        await Result.TryAsync(
                operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(_connection, UserRepositorySql.GetUserByEmail, new { Email = email }),
                errorFactory: UserError (ex) => new GetByEmailAsyncError(ex.Message, ex)
            );

    public async Task<Result<User?, UserError>> GetByIdAsync(Guid userId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(_connection, UserRepositorySql.GetById, new { Userid = userId }),
            errorFactory: UserError (ex) => new GetByIdAsyncError(ex.Message, ex)
        );

    public async Task<Result<User?, UserError>> GetByRefreshTokenAsync(string refreshToken) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(_connection, UserRepositorySql.GetByRefreshToken, new { RefreshToken = refreshToken, CurrentDate = DateTime.UtcNow  }),
            errorFactory: UserError (ex) => new GetByRefreshTokenAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, UserError>> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, UserRepositorySql.UpdateRefreshToken, new
            {
                UserId = userId,
                RefreshToken = refreshToken,
                ExpirationDate = expirationDate,
                UpdatedDate = DateTime.UtcNow
            }),
            errorFactory: UserError (ex) => new UpdateRefreshTokenAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<UserError>(
            affectedRows,
            msg => new UpdateRefreshTokenAsyncError(msg),
            "No refresh token was updated."
        ));

    public async Task<Result<Unit, UserError>> ClearRefreshTokenAsync(Guid userId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, UserRepositorySql.DeleteRefreshToken, new
            {
                UserId = userId,
                UpdatedDate = DateTime.UtcNow
            }),
            errorFactory: UserError (ex) => new ClearRefreshTokenAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<UserError>(
            affectedRows,
            msg => new ClearRefreshTokenAsyncError(msg),
            "No refresh token was deleted."
        ));

    public async Task<Result<Company?, UserError>> GetUserFirstCompanyAsync(Guid userId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, Company?>(_connection, UserRepositorySql.GetUserFirstCompany, new { UserId = userId }),
            errorFactory: UserError (ex) => new GetUserFirstCompanyAsyncError(ex.Message, ex)
        );

    public async Task<Result<bool, UserError>> ValidateUserCompanyOwnershipAsync(Guid userId, Guid companyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, bool>(_connection, UserRepositorySql.ValidateUserCompanyOwnership, new { UserId = userId, CompanyId = companyId }),
            errorFactory: UserError (ex) => new ValidateUserCompanyOwnershipAsyncError(ex.Message, ex)
        );
}