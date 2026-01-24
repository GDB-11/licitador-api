using BindSharp;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.Company;

namespace Infrastructure.Core.Interfaces.Account;

public interface IUserRepository
{
    Task<Result<User?, UserError>> GetByEmailAsync(string email);
    Task<Result<User?, UserError>> GetByIdAsync(Guid userId);
    Task<Result<User?, UserError>> GetByRefreshTokenAsync(string refreshToken);
    Task<Result<Unit, UserError>> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate);
    Task<Result<Unit, UserError>> ClearRefreshTokenAsync(Guid userId);
    Task<Result<Company?, UserError>> GetUserFirstCompanyAsync(Guid userId);
    Task<Result<bool, UserError>> ValidateUserCompanyOwnershipAsync(Guid userId, Guid companyId);
}