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
using GetActiveBankAccountIdAsyncError = Application.Core.DTOs.Company.Errors.GetActiveBankAccountIdAsyncError;
using GetActiveLegalRepresentativeIdAsyncError = Application.Core.DTOs.Company.Errors.GetActiveLegalRepresentativeIdAsyncError;
using GetCompanyDetailsAsyncError = Application.Core.DTOs.Company.Errors.GetCompanyDetailsAsyncError;
using InsertBankAccountAsyncError = Application.Core.DTOs.Company.Errors.InsertBankAccountAsyncError;
using InsertLegalRepresentativeAsyncError = Application.Core.DTOs.Company.Errors.InsertLegalRepresentativeAsyncError;
using UpdateBankAccountAsyncError = Application.Core.DTOs.Company.Errors.UpdateBankAccountAsyncError;
using UpdateCompanyAsyncError = Application.Core.DTOs.Company.Errors.UpdateCompanyAsyncError;
using UpdateLegalRepresentativeAsyncError = Application.Core.DTOs.Company.Errors.UpdateLegalRepresentativeAsyncError;

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
                .EnsureNotNullAsync(new CompanyNotFoundError())
                .BindAsync(MapCompanyDetailsToResponse));

    public Task<Result<Unit, CompanyDomainError>> UpdateCompanyDetailsAsync(Guid userId, UpdateCompanyDetailsRequest request) =>
        MapRequestToInfrastructureModels(request)
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
                    .BindAsync(async _ => mapped.LegalRepresentative is not null
                        ? await UpdateOrInsertLegalRepresentativeAsync(request.CompanyId, mapped.LegalRepresentative)
                        : Unit.Value)
                    .BindAsync(async _ => mapped.BankAccount is not null
                        ? await UpdateOrInsertBankAccountAsync(request.CompanyId, mapped.BankAccount)
                        : Unit.Value)));

    public async Task<Result<CompanyStatisticsResponse, CompanyDomainError>> GetCompanyStatisticsAsync(Guid userId, Guid companyId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .MapErrorAsync(CompanyDomainError (error) => new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _companyRepository.GetCompanyDetailsAsync(companyId)
                .MapErrorAsync(CompanyDomainError (error) => new GetCompanyDetailsAsyncError(error.Message, error.Details, error.Exception))
                .EnsureNotNullAsync(new CompanyNotFoundError())
                .MapAsync(companyDetails => new CompanyStatisticsResponse
                {
                    GeneratedDocuments = 0, // You'll need to populate this
                    CompleteProfilePercentage = CalculateProfileCompletion(companyDetails.Company, companyDetails.LegalRepresentative, companyDetails.BankAccount),
                    ConsortiaCompanies = GetConsortiaCompanies(companyDetails.Company.CompanyId)
                }));

    private static Task<Result<UserCompanyDetailsResponse, CompanyDomainError>> MapCompanyDetailsToResponse(CompanyDetails details)
    {
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

    private static byte CalculateProfileCompletion(Infrastructure.Core.Models.Company.Company? company, LegalRepresentative? legalRepresentative, BankAccount? bankAccount)
    {
        object?[] fields =
        [
            company?.Ruc,
            company?.RazonSocial,
            company?.DomicilioLegal,
            company?.Telefono,
            company?.Email,
            company?.FechaConstitucion,
            legalRepresentative?.DocumentNumber,
            legalRepresentative?.FullName,
            legalRepresentative?.NationalIdImage,
            bankAccount?.CciCode,
            bankAccount?.AccountNumber,
            bankAccount?.BankName
        ];

        int completedCount = fields.Count(IsFieldComplete);
        return (byte)Math.Round((double)completedCount / fields.Length * 100);
    }

    private static bool IsFieldComplete(object? field) => field switch
    {
        string str => !string.IsNullOrWhiteSpace(str),
        null => false,
        _ => true
    };

    private int GetConsortiaCompanies(Guid companyId)
    {
        return 0;
    }
}