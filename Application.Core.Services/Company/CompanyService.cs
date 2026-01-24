using Application.Core.DTOs.Company.DTO;
using Application.Core.DTOs.Company.Errors;
using Application.Core.DTOs.Company.Request;
using Application.Core.DTOs.Company.Response;
using Application.Core.Interfaces.Company;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Organization;
using Infrastructure.Core.Models.Company;

namespace Application.Core.Services.Company;

public sealed class CompanyService : ICompany
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;

    public CompanyService(IUserRepository userRepository, ICompanyRepository companyRepository)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
    }

    public Task<Result<UserCompanyResponse, CompanyDomainError>> GetUserCompanyAsync(Guid userId) =>
        _userRepository.GetUserFirstCompanyAsync(userId)
            .MapErrorAsync(CompanyDomainError (error) => new GetUserFirstCompanyAsyncError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new CompanyNotFoundError())
            .MapAsync(company => new UserCompanyResponse
            {
                CompanyId = company.CompanyId,
                Ruc = company.Ruc,
                RazonSocial = company.RazonSocial
            });

    public Task<Result<UserCompanyDetailsResponse, CompanyDomainError>> GetUserCompanyDetailsAsync(Guid userId, Guid companyId) =>
        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .MapErrorAsync(CompanyDomainError (error) => new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _companyRepository.GetCompanyDetailsAsync(companyId)
                .MapErrorAsync(CompanyDomainError (error) => new GetCompanyDetailsAsyncError(error.Message, error.Details, error.Exception))
                .BindAsync(MapCompanyDetailsToResponse));

    public Task<Result<Unit, CompanyDomainError>> UpdateCompanyDetailsAsync(Guid userId, UpdateCompanyDetailsRequest request) =>
        Task.FromResult(MapRequestToInfrastructureModels(request))
            .BindAsync(mapped => _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
                .MapErrorAsync(CompanyDomainError (error) => new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
                .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
                .BindAsync(_ => _companyRepository.UpdateCompanyAsync(
                        request.CompanyId,
                        request.Ruc,
                        request.RazonSocial,
                        request.DomicilioLegal,
                        request.Telefono,
                        request.Email,
                        request.FechaConstitucion,
                        request.IsMype)
                    .MapErrorAsync(CompanyDomainError (error) => new UpdateCompanyAsyncError(error.Message, error.Details, error.Exception))
                    .BindAsync(_ => mapped.LegalRepresentative is not null
                        ? UpdateOrInsertLegalRepresentativeAsync(request.CompanyId, mapped.LegalRepresentative)
                        : Task.FromResult(Result<Unit, CompanyDomainError>.Success(Unit.Value)))
                    .BindAsync(_ => mapped.BankAccount is not null
                        ? UpdateOrInsertBankAccountAsync(request.CompanyId, mapped.BankAccount)
                        : Task.FromResult(Result<Unit, CompanyDomainError>.Success(Unit.Value)))));

    private static Task<Result<UserCompanyDetailsResponse, CompanyDomainError>> MapCompanyDetailsToResponse(CompanyDetails? details)
    {
        if (details?.Company is null)
        {
            return Task.FromResult(Result<UserCompanyDetailsResponse, CompanyDomainError>.Failure(new CompanyNotFoundError()));
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

        return Task.FromResult(Result<UserCompanyDetailsResponse, CompanyDomainError>.Success(response));
    }

    private static Result<(LegalRepresentativeUpdate? LegalRepresentative, BankAccountUpdate? BankAccount), CompanyDomainError> MapRequestToInfrastructureModels(
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

            return (legalRepUpdate, bankAccountUpdate);
        }
        catch (FormatException ex)
        {
            return new InvalidBase64ImageFormatError($"Invalid Base64 format for National ID Image.", ex.Message, ex);
        }
    }

    private Task<Result<Unit, CompanyDomainError>> UpdateOrInsertLegalRepresentativeAsync(Guid companyId, LegalRepresentativeUpdate legalRepUpdate) =>
        _companyRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .MapErrorAsync(CompanyDomainError (error) => new GetActiveLegalRepresentativeIdAsyncError(error.Message, error.Details, error.Exception))
            .BindAsync(existingId => existingId.HasValue
                ? _companyRepository.UpdateLegalRepresentativeAsync(
                    existingId.Value,
                    legalRepUpdate.FullName,
                    legalRepUpdate.DocumentType,
                    legalRepUpdate.DocumentNumber,
                    legalRepUpdate.NationalIdImage)
                    .MapErrorAsync(CompanyDomainError (error) => new UpdateLegalRepresentativeAsyncError(error.Message, error.Details, error.Exception))
                : _companyRepository.InsertLegalRepresentativeAsync(
                    companyId,
                    legalRepUpdate.FullName,
                    legalRepUpdate.DocumentType,
                    legalRepUpdate.DocumentNumber,
                    legalRepUpdate.NationalIdImage)
                    .MapErrorAsync(CompanyDomainError (error) => new InsertLegalRepresentativeAsyncError(error.Message, error.Details, error.Exception))
                    .MapAsync(_ => Unit.Value));

    private Task<Result<Unit, CompanyDomainError>> UpdateOrInsertBankAccountAsync(Guid companyId, BankAccountUpdate bankAccountUpdate) =>
        _companyRepository.GetActiveBankAccountIdAsync(companyId)
            .MapErrorAsync(CompanyDomainError (error) => new GetActiveBankAccountIdAsyncError(error.Message, error.Details, error.Exception))
            .BindAsync(existingId => existingId.HasValue
                ? _companyRepository.UpdateBankAccountAsync(
                    existingId.Value,
                    bankAccountUpdate.BankName,
                    bankAccountUpdate.AccountNumber,
                    bankAccountUpdate.CciCode)
                    .MapErrorAsync(CompanyDomainError (error) => new UpdateBankAccountAsyncError(error.Message, error.Details, error.Exception))
                : _companyRepository.InsertBankAccountAsync(
                    companyId,
                    bankAccountUpdate.BankName,
                    bankAccountUpdate.AccountNumber,
                    bankAccountUpdate.CciCode)
                    .MapErrorAsync(CompanyDomainError (error) => new InsertBankAccountAsyncError(error.Message, error.Details, error.Exception))
                    .MapAsync(_ => Unit.Value));
}