namespace Application.Core.DTOs.Company.Errors;

public abstract record CompanyDomainError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetUserFirstCompanyAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record CompanyNotFoundError()
    : CompanyDomainError("No company was found for the user.");

public sealed record ValidateUserCompanyOwnershipAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);

public sealed record GetCompanyDetailsAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record UserCompanyOwnershipError()
    : CompanyDomainError("The user does not have access to this company.");
    
public sealed record UpdateCompanyAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record InvalidBase64ImageFormatError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record GetActiveLegalRepresentativeIdAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record UpdateLegalRepresentativeAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record InsertLegalRepresentativeAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record GetActiveBankAccountIdAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);
    
public sealed record InsertBankAccountAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);

public sealed record UpdateBankAccountAsyncError(string Message, string? Details = null, Exception? Exception = null)
    : CompanyDomainError(Message, Details, Exception);