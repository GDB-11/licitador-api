using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Organization;
using Infrastructure.Core.Interfaces.Organization;
using Infrastructure.Core.Models.Company;

namespace Infrastructure.Core.Services.Organization;

public sealed class CompanyRepository : BaseDatabaseService, ICompanyRepository
{
    private readonly IDbConnection _connection;

    public CompanyRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<Result<CompanyDetails?, CompanyError>> GetCompanyDetailsAsync(Guid companyId) =>
        await Result.TryAsync(
                async () => await _connection.QueryAsync<Company, LegalRepresentative?, BankAccount?, CompanyDetails>(
                    CompanyRepositorySql.GetCompanyDetails,
                    (company, legalRep, bankAccount) => new CompanyDetails
                    {
                        Company = company,
                        LegalRepresentative = legalRep,
                        BankAccount = bankAccount
                    },
                    new { CompanyId = companyId },
                    splitOn: "LegalRepresentativeId,BankAccountId"
                ),
            CompanyError (ex) => new GetCompanyDetailsAsyncError(ex.Message, ex)
        )
        .MapAsync(result => result.FirstOrDefault());

    public async Task<Result<Unit, CompanyError>> UpdateCompanyAsync(Guid companyId, string ruc, string razonSocial, string domicilioLegal, string? telefono,
        string email, DateTime? fechaConstitucion, bool isMype) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, CompanyRepositorySql.UpdateCompany, new
            {
                CompanyId = companyId,
                Ruc = ruc,
                RazonSocial = razonSocial,
                DomicilioLegal = domicilioLegal,
                Telefono = telefono,
                Email = email,
                FechaConstitucion = fechaConstitucion,
                IsMype = isMype,
                UpdatedDate = DateTime.UtcNow
            }),
            errorFactory: CompanyError (ex) => new UpdateCompanyAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CompanyError>(
            affectedRows,
            msg => new UpdateCompanyAsyncError(msg),
            "The company was not updated."
        ));

    public async Task<Result<Guid?, CompanyError>> GetActiveLegalRepresentativeIdAsync(Guid companyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, Guid?>(_connection, CompanyRepositorySql.GetActiveLegalRepresentativeId, new { CompanyId = companyId }),
            errorFactory: CompanyError (ex) => new GetActiveLegalRepresentativeIdAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, CompanyError>> UpdateLegalRepresentativeAsync(Guid legalRepresentativeId, string fullName, string documentType,
        string documentNumber, byte[]? nationalIdImage) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, CompanyRepositorySql.UpdateLegalRepresentative, new
            {
                LegalRepresentativeId = legalRepresentativeId,
                FullName = fullName,
                DocumentType = documentType,
                DocumentNumber = documentNumber,
                NationalIdImage = nationalIdImage,
                UpdatedDate = DateTime.UtcNow
            }),
            errorFactory: CompanyError (ex) => new UpdateLegalRepresentativeAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CompanyError>(
            affectedRows,
            msg => new UpdateLegalRepresentativeAsyncError(msg),
            "No legal representative was updated."
        ));

    public async Task<Result<Guid, CompanyError>> InsertLegalRepresentativeAsync(Guid companyId, string fullName, string documentType, string documentNumber,
        byte[]? nationalIdImage)
    {
        var legalRepresentativeId = Guid.CreateVersion7();
        
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection,
                CompanyRepositorySql.InsertLegalRepresentative, new
                {
                    LegalRepresentativeId = legalRepresentativeId,
                    CompanyId = companyId,
                    FullName = fullName,
                    DocumentType = documentType,
                    DocumentNumber = documentNumber,
                    NationalIdImage = nationalIdImage,
                    CreatedDate = DateTime.UtcNow
                }),
            errorFactory: CompanyError (ex) => new InsertLegalRepresentativeAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CompanyError>(
            affectedRows,
            msg => new InsertLegalRepresentativeAsyncError(msg),
            "No legal representative was inserted."
        ));

        return legalRepresentativeId;
    }

    public async Task<Result<Guid?, CompanyError>> GetActiveBankAccountIdAsync(Guid companyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, Guid?>(_connection, CompanyRepositorySql.GetActiveBankAccountId, new { CompanyId = companyId }),
            errorFactory: CompanyError (ex) => new GetActiveBankAccountIdAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, CompanyError>> UpdateBankAccountAsync(Guid bankAccountId, string bankName, string accountNumber, string cciCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, CompanyRepositorySql.UpdateBankAccount, new
            {
                BankAccountId = bankAccountId,
                BankName = bankName,
                AccountNumber = accountNumber,
                CciCode = cciCode,
                UpdatedDate = DateTime.UtcNow
            }),
            errorFactory: CompanyError (ex) => new UpdateBankAccountAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CompanyError>(
            affectedRows,
            msg => new UpdateBankAccountAsyncError(msg),
            "No bank account was updated."
        ));

    public async Task<Result<Guid, CompanyError>> InsertBankAccountAsync(Guid companyId, string bankName, string accountNumber, string cciCode)
    {
        var bankAccountId = Guid.CreateVersion7();
        
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection,
                CompanyRepositorySql.InsertBankAccount, new
                {
                    BankAccountId = bankAccountId,
                    CompanyId = companyId,
                    BankName = bankName,
                    AccountNumber = accountNumber,
                    CciCode = cciCode,
                    CreatedDate = DateTime.UtcNow
                }),
            errorFactory: CompanyError (ex) => new InsertBankAccountAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CompanyError>(
            affectedRows,
            msg => new InsertBankAccountAsyncError(msg),
            "No bank account was inserted."
        ));

        return bankAccountId;
    }
}