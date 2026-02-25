using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Consortium;
using Infrastructure.Core.Interfaces.Consortium;
using Infrastructure.Core.Models.Consortium;

namespace Infrastructure.Core.Services.Consortium;

public sealed class ConsortiumRepository: BaseDatabaseService, IConsortiumRepository
{
    private readonly IDbConnection _connection;

    public ConsortiumRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Result<List<ConsortiumCompany>, ConsortiumError>> GetAllCompaniesAsync(Guid companyId) =>
        await Result.TryAsync(
            async () => await _connection.QueryAsync<ConsortiumCompany, ConsortiumCompanyLegalRepresentative?, ConsortiumCompany>(
                    ConsortiumRepositorySql.GetAllConsortiumCompanies,
                    (company, legalRep) => company with { LegalRepresentative = legalRep },
                    new { CompanyId = companyId },
                    splitOn: "ConsortiumLegalRepresentativeId"
                ),
            ConsortiumError (ex) => new GetAllCompaniesAsyncError(ex.Message, ex)
        )
        .MapAsync(result => result.ToList());
    
    public async Task<Result<int, ConsortiumError>> GetNumberOfActiveConsortiumCompanies(Guid companyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, int>(_connection, ConsortiumRepositorySql.GetNumberOfActiveConsortiumCompanies, new { CompanyId = companyId }),
            errorFactory: ConsortiumError (ex) => new GetNumberOfActiveConsortiumCompaniesAsyncError(ex.Message, ex)
        );
    
    public async Task<Result<bool, ConsortiumError>> ValidateCompanyConsortiumOwnershipAsync(Guid companyId, Guid consortiumCompanyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, bool>(_connection, ConsortiumRepositorySql.ValidateCompanyConsortiumOwnership, new { CompanyId = companyId, ConsortiumCompanyId = consortiumCompanyId }),
            errorFactory: ConsortiumError (ex) => new ValidateCompanyConsortiumOwnershipAsync(ex.Message, ex)
        );
    
    public async Task<Result<ConsortiumCompany?, ConsortiumError>> GetCompanyAsync(Guid consortiumCompanyId) =>
        await Result.TryAsync(
            async () => await _connection.QueryAsync<ConsortiumCompany, ConsortiumCompanyLegalRepresentative?, ConsortiumCompany>(
                ConsortiumRepositorySql.GetConsortiumCompany,
                (company, legalRep) => company with { LegalRepresentative = legalRep },
                new { ConsortiumCompanyId = consortiumCompanyId },
                splitOn: "ConsortiumLegalRepresentativeId"
                ),
                ConsortiumError (ex) => new GetAllCompaniesAsyncError(ex.Message, ex)
            )
            .MapAsync(result => result.SingleOrDefault());

    public async Task<Result<Unit, ConsortiumError>>
        InsertConsortiumCompanyAsync(CreateConsortiumCompany consortiumCompany) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.InsertConsortiumCompany, new
            {
                consortiumCompany.ConsortiumCompanyId,
                consortiumCompany.CompanyId,
                consortiumCompany.Ruc,
                consortiumCompany.RnpRegistration,
                consortiumCompany.RazonSocial,
                consortiumCompany.NombreComercial,
                consortiumCompany.RnpValidUntil,
                consortiumCompany.MainActivity,
                consortiumCompany.DomicilioFiscal,
                consortiumCompany.ContactPhone,
                consortiumCompany.ContactEmail
            }),
            errorFactory: ConsortiumError (ex) => new InsertConsortiumCompanyAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new InsertConsortiumCompanyAsyncError(msg),
            "No consortium company was inserted."
        ));
    
    public async Task<Result<Unit, ConsortiumError>>
        UpdateConsortiumCompanyAsync(ConsortiumCompany consortiumCompany) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.UpdateConsortiumCompany, new
            {
                consortiumCompany.ConsortiumCompanyId,
                consortiumCompany.Ruc,
                consortiumCompany.RnpRegistration,
                consortiumCompany.RazonSocial,
                consortiumCompany.NombreComercial,
                consortiumCompany.RnpValidUntil,
                consortiumCompany.MainActivity,
                consortiumCompany.DomicilioFiscal,
                consortiumCompany.ContactPhone,
                consortiumCompany.ContactEmail
            }),
            errorFactory: ConsortiumError (ex) => new UpdateConsortiumCompanyAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new UpdateConsortiumCompanyAsyncError(msg),
            "No consortium company was updated."
        ));
    
    public async Task<Result<Unit, ConsortiumError>>
        InsertConsortiumLegalRepresentativeAsync(ConsortiumCompanyLegalRepresentative consortiumCompanyLegalRepresentative) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.InsertConsortiumLegalRepresentative, new
            {
                consortiumCompanyLegalRepresentative.ConsortiumLegalRepresentativeId,
                consortiumCompanyLegalRepresentative.ConsortiumCompanyId,
                consortiumCompanyLegalRepresentative.Dni,
                consortiumCompanyLegalRepresentative.FullName,
                consortiumCompanyLegalRepresentative.Position
            }),
            errorFactory: ConsortiumError (ex) => new InsertLegalRepresentativeAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new InsertLegalRepresentativeAsyncError(msg),
            "No consortium company legal representative was inserted."
        ));
    
    public async Task<Result<Unit, ConsortiumError>>
        UpdateConsortiumLegalRepresentativeAsync(ConsortiumCompanyLegalRepresentative consortiumCompanyLegalRepresentative) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.UpdateConsortiumLegalRepresentative, new
            {
                consortiumCompanyLegalRepresentative.ConsortiumLegalRepresentativeId,
                consortiumCompanyLegalRepresentative.Dni,
                consortiumCompanyLegalRepresentative.FullName,
                consortiumCompanyLegalRepresentative.Position
            }),
            errorFactory: ConsortiumError (ex) => new UpdateLegalRepresentativeAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new UpdateLegalRepresentativeAsyncError(msg),
            "No consortium company legal representative was updated."
        ));
    
    public async Task<Result<Unit, ConsortiumError>> DeleteConsortiumCompanyAsync(Guid consortiumCompanyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.DeleteConsortiumCompany, new
            {
                ConsortiumCompanyId = consortiumCompanyId
            }),
            errorFactory: ConsortiumError (ex) => new DeleteConsortiumCompanyAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new DeleteConsortiumCompanyAsyncError(msg),
            "No consortium company was deleted."
        ));
    
    public async Task<Result<Unit, ConsortiumError>> DeleteConsortiumLegalRepresentativeAsync(Guid consortiumCompanyId) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(_connection, ConsortiumRepositorySql.DeleteConsortiumLegalRepresentative, new
            {
                ConsortiumCompanyId = consortiumCompanyId
            }),
            errorFactory: ConsortiumError (ex) => new DeleteLegalRepresentativeAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<ConsortiumError>(
            affectedRows,
            msg => new DeleteLegalRepresentativeAsyncError(msg),
            "No consortium company legal representatives were deleted."
        ));
}