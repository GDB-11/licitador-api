using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.Company;

namespace Infrastructure.Core.Interfaces.Account;

public interface IUserRepository
{
    Task<Result<User?, GenericError>> GetByEmailAsync(string email);
    Task<Result<User?, GenericError>> GetByIdAsync(Guid userId);
    Task<Result<User?, GenericError>> GetByRefreshTokenAsync(string refreshToken);
    Task<Result<Unit, GenericError>> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate);
    Task<Result<Unit, GenericError>> ClearRefreshTokenAsync(Guid userId);
    Task<Result<Company?, GenericError>> GetUserFirstCompanyAsync(Guid userId);
}