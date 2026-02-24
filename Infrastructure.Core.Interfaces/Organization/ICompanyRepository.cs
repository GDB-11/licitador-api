using BindSharp;
using Infrastructure.Core.DTOs.Organization;
using Infrastructure.Core.Models.Company;

namespace Infrastructure.Core.Interfaces.Organization;

public interface ICompanyRepository
{
    Task<Result<CompanyDetails?, CompanyError>> GetCompanyDetailsAsync(Guid companyId);
    Task<Result<CompanyDetails?, CompanyError>> GetCompanyDetailsByConsortiumCompanyIdAsync(string consortiumCompanyId);
    Task<Result<Unit, CompanyError>> UpdateCompanyAsync(
        Guid companyId,
        string ruc,
        string razonSocial,
        string domicilioLegal,
        string? telefono,
        string email,
        DateTime? fechaConstitucion,
        bool isMype);
    Task<Result<Guid?, CompanyError>> GetActiveLegalRepresentativeIdAsync(Guid companyId);
    Task<Result<Unit, CompanyError>> UpdateLegalRepresentativeAsync(
        Guid legalRepresentativeId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage);
    Task<Result<Guid, CompanyError>> InsertLegalRepresentativeAsync(
        Guid companyId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage);
    Task<Result<Guid?, CompanyError>> GetActiveBankAccountIdAsync(Guid companyId);
    Task<Result<Unit, CompanyError>> UpdateBankAccountAsync(
        Guid bankAccountId,
        string bankName,
        string accountNumber,
        string cciCode);
    Task<Result<Guid, CompanyError>> InsertBankAccountAsync(
        Guid companyId,
        string bankName,
        string accountNumber,
        string cciCode);
}