using Application.Core.DTOs.Consortium.Errors;
using Application.Core.DTOs.Consortium.Request;
using Application.Core.DTOs.Consortium.Response;
using BindSharp;

namespace Application.Core.Interfaces.Consortium;

public interface IConsortium
{
    Task<Result<GetAllCompaniesResponse, ConsortiumDomainError>> GetAllCompaniesAsync(GetAllCompaniesRequest request,
        Guid userId);

    Task<Result<GetCompanyDataResponse, ConsortiumDomainError>> GetCompanyDataAsync(
        GetCompanyDataRequest request, Guid userId);

    Task<Result<Unit, ConsortiumDomainError>> CreateConsortiumCompanyAsync(
        CreateConsortiumCompanyRequest request, Guid userId);

    Task<Result<Unit, ConsortiumDomainError>> UpdateConsortiumCompanyAsync(
        UpdateConsortiumCompanyRequest request, Guid userId);

    Task<Result<Unit, ConsortiumDomainError>> DeleteConsortiumCompanyAsync(
        DeleteConsortiumCompanyRequest request, Guid userId);
}