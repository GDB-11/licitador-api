using Application.Core.DTOs.Consortium.DTO;
using Application.Core.DTOs.Consortium.Errors;
using Application.Core.DTOs.Consortium.Request;
using Application.Core.DTOs.Consortium.Response;
using Application.Core.Helpers.Consortium;
using Application.Core.Interfaces.Consortium;
using BindSharp;
using BindSharp.Extensions;
using Global.Helpers.Date;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Consortium;

namespace Application.Core.Services.Consortium;

public sealed class ConsortiumService : IConsortium
{
    private readonly IUserRepository _userRepository;
    private readonly IConsortiumRepository _consortiumRepository;

    public ConsortiumService(IConsortiumRepository consortiumRepository, IUserRepository userRepository)
    {
        _consortiumRepository = consortiumRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetAllCompaniesResponse, ConsortiumDomainError>> GetAllCompaniesAsync(
        GetAllCompaniesRequest request, Guid userId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
            .MapErrorAsync(ConsortiumDomainError (error) =>
                new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _consortiumRepository.GetAllCompaniesAsync(request.CompanyId)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new GetAllCompaniesAsyncError(error.Message, error.Details, error.Exception)))
            .MapAsync(companies => new GetAllCompaniesResponse
            {
                Companies = companies
            });

    public async Task<Result<GetCompanyDataResponse, ConsortiumDomainError>> GetCompanyDataAsync(
        GetCompanyDataRequest request, Guid userId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
            .MapErrorAsync(ConsortiumDomainError (error) =>
                new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _consortiumRepository
                .ValidateCompanyConsortiumOwnershipAsync(request.CompanyId, request.ConsortiumCompanyId)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new ValidateCompanyConsortiumOwnershipAsyncError(error.Message, error.Details, error.Exception)))
            .EnsureAsync(hasCompanyOwnership => hasCompanyOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _consortiumRepository.GetCompanyAsync(request.ConsortiumCompanyId)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new GetAllCompaniesAsyncError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new NoConsortiumCompanyFound())
            .MapAsync(consortiumCompany => new GetCompanyDataResponse
            {
                Company = new ConsortiumCompanyData
                {
                    ConsortiumCompanyId = consortiumCompany.ConsortiumCompanyId,
                    CompanyId = consortiumCompany.CompanyId,
                    Ruc = consortiumCompany.Ruc,
                    RnpRegistration = consortiumCompany.RnpRegistration,
                    RazonSocial = consortiumCompany.RazonSocial,
                    NombreComercial = consortiumCompany.NombreComercial,
                    RnpValidUntil = consortiumCompany.RnpValidUntil,
                    MainActivity = consortiumCompany.MainActivity,
                    DomicilioFiscal = consortiumCompany.DomicilioFiscal,
                    ContactPhone = consortiumCompany.ContactPhone,
                    ContactEmail = consortiumCompany.ContactEmail,
                    IsActive = consortiumCompany.IsActive,
                    UpdatedDate = consortiumCompany.UpdatedDate,
                    LegalRepresentative = new ConsortiumCompanyLegalRepresentativeData
                    {
                        ConsortiumLegalRepresentativeId =
                            consortiumCompany.LegalRepresentative?.ConsortiumLegalRepresentativeId,
                        ConsortiumCompanyId = consortiumCompany.LegalRepresentative?.ConsortiumCompanyId,
                        Dni = consortiumCompany.LegalRepresentative?.Dni,
                        FullName = consortiumCompany.LegalRepresentative?.FullName,
                        Position = consortiumCompany.LegalRepresentative?.Position,
                        IsActive = consortiumCompany.LegalRepresentative?.IsActive
                    }
                }
            });

    public async Task<Result<Unit, ConsortiumDomainError>> CreateConsortiumCompanyAsync(
        CreateConsortiumCompanyRequest request, Guid userId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
            .MapErrorAsync(ConsortiumDomainError (error) =>
                new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .MapAsync(_ => request.ToDatabaseObject(Guid.CreateVersion7(), Guid.CreateVersion7()))
            .BindAsync(consortiumCompany => _consortiumRepository.InsertConsortiumCompanyAsync(consortiumCompany)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new InsertConsortiumCompanyAsyncError(error.Message, error.Details, error.Exception))
                .BindAsync(_ => _consortiumRepository
                    .InsertConsortiumLegalRepresentativeAsync(consortiumCompany.LegalRepresentative)
                    .MapErrorAsync(ConsortiumDomainError (error) =>
                        new InsertLegalRepresentativeAsyncError(error.Message, error.Details, error.Exception))));

    public async Task<Result<Unit, ConsortiumDomainError>> UpdateConsortiumCompanyAsync(
        UpdateConsortiumCompanyRequest request, Guid userId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
            .MapErrorAsync(ConsortiumDomainError (error) =>
                new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .MapAsync(_ => request.ToDatabaseObject())
            .BindAsync(consortiumCompany => _consortiumRepository.UpdateConsortiumCompanyAsync(consortiumCompany)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new UpdateConsortiumCompanyAsyncError(error.Message, error.Details, error.Exception))
                .BindAsync(_ => _consortiumRepository
                    .UpdateConsortiumLegalRepresentativeAsync(consortiumCompany.LegalRepresentative)
                    .MapErrorAsync(ConsortiumDomainError (error) =>
                        new UpdateLegalRepresentativeAsyncError(error.Message, error.Details, error.Exception))));

    public async Task<Result<Unit, ConsortiumDomainError>> DeleteConsortiumCompanyAsync(
        DeleteConsortiumCompanyRequest request, Guid userId) =>
        await _userRepository.ValidateUserCompanyOwnershipAsync(userId, request.CompanyId)
            .MapErrorAsync(ConsortiumDomainError (error) =>
                new ValidateUserCompanyOwnershipAsyncError(error.Message, error.Details, error.Exception))
            .EnsureAsync(hasOwnership => hasOwnership, new UserCompanyOwnershipError())
            .BindAsync(_ => _consortiumRepository.DeleteConsortiumLegalRepresentativeAsync(request.ConsortiumCompanyId)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new DeleteConsortiumCompanyAsyncError(error.Message, error.Details, error.Exception)))
            .BindAsync(_ => _consortiumRepository.DeleteConsortiumCompanyAsync(request.ConsortiumCompanyId)
                .MapErrorAsync(ConsortiumDomainError (error) =>
                    new DeleteLegalRepresentativeAsyncError(error.Message, error.Details, error.Exception)));
}