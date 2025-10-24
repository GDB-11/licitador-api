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
    Task<Result<bool, GenericError>> ValidateUserCompanyOwnershipAsync(Guid userId, Guid companyId);
    Task<Result<CompanyDetails?, GenericError>> GetCompanyDetailsAsync(Guid companyId);
    Task<Result<Unit, GenericError>> UpdateCompanyAsync(
        Guid companyId,
        string ruc,
        string razonSocial,
        string domicilioLegal,
        string? telefono,
        string email,
        DateTime? fechaConstitucion,
        bool isMype);
    Task<Result<Guid?, GenericError>> GetActiveLegalRepresentativeIdAsync(Guid companyId);
    Task<Result<Unit, GenericError>> UpdateLegalRepresentativeAsync(
        Guid legalRepresentativeId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage);
    Task<Result<Guid, GenericError>> InsertLegalRepresentativeAsync(
        Guid companyId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage);
    Task<Result<Guid?, GenericError>> GetActiveBankAccountIdAsync(Guid companyId);
    Task<Result<Unit, GenericError>> UpdateBankAccountAsync(
        Guid bankAccountId,
        string bankName,
        string accountNumber,
        string cciCode);
    Task<Result<Guid, GenericError>> InsertBankAccountAsync(
        Guid companyId,
        string bankName,
        string accountNumber,
        string cciCode);
}