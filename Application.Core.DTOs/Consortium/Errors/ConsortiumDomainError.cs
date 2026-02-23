namespace Application.Core.DTOs.Consortium.Errors;

public abstract record ConsortiumDomainError(string Message, string? Details = null, Exception? Exception = null);

public sealed record ValidateUserCompanyOwnershipAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record UserCompanyOwnershipError()
    : ConsortiumDomainError("The user does not have access to this company.");
    
public sealed record GetAllCompaniesAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record NoConsortiumCompaniesFound()
    : ConsortiumDomainError("The company has no consortium companies.");
    
public sealed record ValidateCompanyConsortiumOwnershipAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record NoConsortiumCompanyFound()
    : ConsortiumDomainError("The consortium company does not exist.");
    
public sealed record InsertConsortiumCompanyAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record InsertLegalRepresentativeAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record UpdateConsortiumCompanyAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record UpdateLegalRepresentativeAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record DeleteConsortiumCompanyAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);
    
public sealed record DeleteLegalRepresentativeAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : ConsortiumDomainError(Message, Details, Exception);