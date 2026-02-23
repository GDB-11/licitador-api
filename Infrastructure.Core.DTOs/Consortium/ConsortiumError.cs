namespace Infrastructure.Core.DTOs.Consortium;

public abstract record ConsortiumError(string Message, string? Details = null, Exception? Exception = null);

public sealed record InsertConsortiumCompanyAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while creating the consortium company.", Details, Exception);

public sealed record InsertLegalRepresentativeAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while creating the consortium company legal representative.", Details, Exception);
    
public sealed record UpdateConsortiumCompanyAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while updating the consortium company.", Details, Exception);
    
public sealed record UpdateLegalRepresentativeAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while updating the consortium company legal representative.", Details, Exception);
    
public sealed record DeleteConsortiumCompanyAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while deleting the consortium company.", Details, Exception);
    
public sealed record DeleteLegalRepresentativeAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while deleting the consortium company legal representative.", Details, Exception);
    
public sealed record GetAllCompaniesAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while retrieving all the consortium companies.", Details, Exception);
    
public sealed record GetNumberOfActiveConsortiumCompaniesAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while retrieving the number of active consortium companies.", Details, Exception);
    
public sealed record GetAsyncError(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while retrieving a the consortium company.", Details, Exception);
    
public sealed record ValidateCompanyConsortiumOwnershipAsync(string? Details = null, Exception? Exception = null)
    : ConsortiumError("An unexpected error occurred while validating the company relationship with the consortium company.", Details, Exception);