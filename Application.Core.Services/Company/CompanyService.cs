using Application.Core.DTOs.Company;
using Application.Core.Interfaces.Company;
using Global.Helpers.Functional;
using Global.Objects.Company;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Company;

namespace Application.Core.Services.Company;

public sealed class CompanyService : ICompany
{
    private readonly IUserRepository _userRepository;

    public CompanyService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Result<UserCompanyResponse, CompanyError>> GetUserCompanyAsync(Guid userId) =>
        _userRepository.GetUserFirstCompanyAsync(userId)
            .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
            .BindAsync(company => company is not null
                ? Task.FromResult(Result<UserCompanyResponse, CompanyError>.Success(new UserCompanyResponse
                {
                    CompanyId = company.CompanyId,
                    Ruc = company.Ruc,
                    RazonSocial = company.RazonSocial
                }))
                : Task.FromResult(Result<UserCompanyResponse, CompanyError>.Failure(new CompanyNotFoundError())));

    public Task<Result<UserCompanyDetailsResponse, CompanyError>> GetUserCompanyDetailsAsync(Guid userId, Guid companyId) =>
        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
            .BindAsync(hasOwnership => hasOwnership
                ? _userRepository.GetCompanyDetailsAsync(companyId)
                    .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                    .BindAsync(details => MapCompanyDetailsToResponse(details))
                : Task.FromResult(Result<UserCompanyDetailsResponse, CompanyError>.Failure(new CompanyUnauthorizedAccessError())));

    public Task<Result<Unit, CompanyError>> UpdateCompanyDetailsAsync(Guid userId, UpdateCompanyDetailsRequest request) =>
        Task.FromResult(MapRequestToInfrastructureModels(request))
            .BindAsync(mapped => _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
                .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                .BindAsync(hasOwnership => hasOwnership
                    ? _userRepository.UpdateCompanyAsync(
                        request.CompanyId,
                        request.Ruc,
                        request.RazonSocial,
                        request.DomicilioLegal,
                        request.Telefono,
                        request.Email,
                        request.FechaConstitucion,
                        request.IsMype)
                        .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                        .BindAsync(_ => mapped.LegalRepresentative is not null
                            ? UpdateOrInsertLegalRepresentativeAsync(request.CompanyId, mapped.LegalRepresentative)
                            : Task.FromResult(Result<Unit, CompanyError>.Success(Unit.Value)))
                        .BindAsync(_ => mapped.BankAccount is not null
                            ? UpdateOrInsertBankAccountAsync(request.CompanyId, mapped.BankAccount)
                            : Task.FromResult(Result<Unit, CompanyError>.Success(Unit.Value)))
                    : Task.FromResult(Result<Unit, CompanyError>.Failure(new CompanyUnauthorizedAccessError()))));

    private static Task<Result<UserCompanyDetailsResponse, CompanyError>> MapCompanyDetailsToResponse(CompanyDetails? details)
    {
        if (details?.Company is null)
        {
            return Task.FromResult(Result<UserCompanyDetailsResponse, CompanyError>.Failure(new CompanyNotFoundError()));
        }

        var response = new UserCompanyDetailsResponse
        {
            CompanyId = details.Company.CompanyId,
            Ruc = details.Company.Ruc,
            RazonSocial = details.Company.RazonSocial,
            DomicilioLegal = details.Company.DomicilioLegal,
            Telefono = details.Company.Telefono,
            Email = details.Company.Email,
            FechaConstitucion = details.Company.FechaConstitucion,
            IsMype = details.Company.IsMype,
            LegalRepresentative = details.LegalRepresentative is not null ? new LegalRepresentativeDto
            {
                LegalRepresentativeId = details.LegalRepresentative.LegalRepresentativeId,
                FullName = details.LegalRepresentative.FullName,
                DocumentType = details.LegalRepresentative.DocumentType,
                DocumentNumber = details.LegalRepresentative.DocumentNumber,
                NationalIdImage = details.LegalRepresentative.NationalIdImage is not null
                    ? Convert.ToBase64String(details.LegalRepresentative.NationalIdImage)
                    : null
            } : null,
            BankAccount = details.BankAccount is not null ? new BankAccountDto
            {
                BankAccountId = details.BankAccount.BankAccountId,
                BankName = details.BankAccount.BankName,
                AccountNumber = details.BankAccount.AccountNumber,
                CciCode = details.BankAccount.CciCode
            } : null
        };

        return Task.FromResult(Result<UserCompanyDetailsResponse, CompanyError>.Success(response));
    }

    private static Result<(LegalRepresentativeUpdate? LegalRepresentative, BankAccountUpdate? BankAccount), CompanyError> MapRequestToInfrastructureModels(
        UpdateCompanyDetailsRequest request)
    {
        try
        {
            var legalRepUpdate = request.LegalRepresentative is not null
                ? new LegalRepresentativeUpdate
                {
                    FullName = request.LegalRepresentative.FullName,
                    DocumentType = request.LegalRepresentative.DocumentType,
                    DocumentNumber = request.LegalRepresentative.DocumentNumber,
                    NationalIdImage = !string.IsNullOrWhiteSpace(request.LegalRepresentative.NationalIdImage)
                        ? Convert.FromBase64String(request.LegalRepresentative.NationalIdImage)
                        : null
                }
                : null;

            var bankAccountUpdate = request.BankAccount is not null
                ? new BankAccountUpdate
                {
                    BankName = request.BankAccount.BankName,
                    AccountNumber = request.BankAccount.AccountNumber,
                    CciCode = request.BankAccount.CciCode
                }
                : null;

            return Result<(LegalRepresentativeUpdate?, BankAccountUpdate?), CompanyError>.Success((legalRepUpdate, bankAccountUpdate));
        }
        catch (FormatException ex)
        {
            return Result<(LegalRepresentativeUpdate?, BankAccountUpdate?), CompanyError>.Failure(
                new CompanyValidationError($"Invalid Base64 format for National ID Image: {ex.Message}"));
        }
    }

    private Task<Result<Unit, CompanyError>> UpdateOrInsertLegalRepresentativeAsync(Guid companyId, LegalRepresentativeUpdate legalRepUpdate) =>
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
            .BindAsync(existingId => existingId.HasValue
                ? _userRepository.UpdateLegalRepresentativeAsync(
                    existingId.Value,
                    legalRepUpdate.FullName,
                    legalRepUpdate.DocumentType,
                    legalRepUpdate.DocumentNumber,
                    legalRepUpdate.NationalIdImage)
                    .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                : _userRepository.InsertLegalRepresentativeAsync(
                    companyId,
                    legalRepUpdate.FullName,
                    legalRepUpdate.DocumentType,
                    legalRepUpdate.DocumentNumber,
                    legalRepUpdate.NationalIdImage)
                    .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                    .MapAsync(_ => Unit.Value));

    private Task<Result<Unit, CompanyError>> UpdateOrInsertBankAccountAsync(Guid companyId, BankAccountUpdate bankAccountUpdate) =>
        _userRepository.GetActiveBankAccountIdAsync(companyId)
            .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
            .BindAsync(existingId => existingId.HasValue
                ? _userRepository.UpdateBankAccountAsync(
                    existingId.Value,
                    bankAccountUpdate.BankName,
                    bankAccountUpdate.AccountNumber,
                    bankAccountUpdate.CciCode)
                    .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                : _userRepository.InsertBankAccountAsync(
                    companyId,
                    bankAccountUpdate.BankName,
                    bankAccountUpdate.AccountNumber,
                    bankAccountUpdate.CciCode)
                    .MapErrorAsync(error => (CompanyError)new CompanyRepositoryError(error.Message, error.Exception))
                    .MapAsync(_ => Unit.Value));
}