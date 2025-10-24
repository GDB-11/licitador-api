using Application.Core.DTOs.Company;
using Global.Objects.Company;
using Global.Objects.Functional;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Company;

public interface ICompany
{
    Task<Result<UserCompanyResponse, CompanyError>> GetUserCompanyAsync(Guid userId);
    Task<Result<UserCompanyDetailsResponse, CompanyError>> GetUserCompanyDetailsAsync(Guid userId, Guid companyId);
    Task<Result<Unit, CompanyError>> UpdateCompanyDetailsAsync(Guid userId, UpdateCompanyDetailsRequest request);
}