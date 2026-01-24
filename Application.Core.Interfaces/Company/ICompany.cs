using Application.Core.DTOs.Company.Errors;
using Application.Core.DTOs.Company.Request;
using Application.Core.DTOs.Company.Response;
using BindSharp;

namespace Application.Core.Interfaces.Company;

public interface ICompany
{
    Task<Result<UserCompanyResponse, CompanyDomainError>> GetUserCompanyAsync(Guid userId);
    Task<Result<UserCompanyDetailsResponse, CompanyDomainError>> GetUserCompanyDetailsAsync(Guid userId, Guid companyId);
    Task<Result<Unit, CompanyDomainError>> UpdateCompanyDetailsAsync(Guid userId, UpdateCompanyDetailsRequest request);
}