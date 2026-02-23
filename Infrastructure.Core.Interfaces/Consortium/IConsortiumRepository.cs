using BindSharp;
using Infrastructure.Core.DTOs.Consortium;
using Infrastructure.Core.Models.Consortium;

namespace Infrastructure.Core.Interfaces.Consortium;

public interface IConsortiumRepository
{
    Task<Result<List<ConsortiumCompany>, ConsortiumError>> GetAllCompaniesAsync(Guid companyId);
    Task<Result<int, ConsortiumError>> GetNumberOfActiveConsortiumCompanies(Guid companyId);
    Task<Result<ConsortiumCompany?, ConsortiumError>> GetCompanyAsync(Guid consortiumCompanyId);

    Task<Result<bool, ConsortiumError>> ValidateCompanyConsortiumOwnershipAsync(Guid companyId,
        Guid consortiumCompanyId);

    Task<Result<Unit, ConsortiumError>>
        InsertConsortiumCompanyAsync(ConsortiumCompany consortiumCompany);

    Task<Result<Unit, ConsortiumError>>
        UpdateConsortiumCompanyAsync(ConsortiumCompany consortiumCompany);

    Task<Result<Unit, ConsortiumError>>
        InsertConsortiumLegalRepresentativeAsync(
            ConsortiumCompanyLegalRepresentative consortiumCompanyLegalRepresentative);

    Task<Result<Unit, ConsortiumError>>
        UpdateConsortiumLegalRepresentativeAsync(
            ConsortiumCompanyLegalRepresentative consortiumCompanyLegalRepresentative);

    Task<Result<Unit, ConsortiumError>> DeleteConsortiumCompanyAsync(Guid consortiumCompanyId);

    Task<Result<Unit, ConsortiumError>> DeleteConsortiumLegalRepresentativeAsync(Guid consortiumCompanyId);
}