using Application.Core.DTOs.Company;
using Application.Core.Interfaces.Company;
using Global.Helpers.Functional;
using Global.Objects.Company;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;

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
}