using Application.Core.DTOs.Company;
using Global.Objects.Company;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Company;

public interface ICompany
{
    Task<Result<UserCompanyResponse, CompanyError>> GetUserCompanyAsync(Guid userId);
}