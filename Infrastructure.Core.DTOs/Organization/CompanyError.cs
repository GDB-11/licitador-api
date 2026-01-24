namespace Infrastructure.Core.DTOs.Organization;

public abstract record CompanyError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetCompanyDetailsAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while retrieving the company details.", Details, Exception);
    
public sealed record UpdateCompanyAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while updating the company.", Details, Exception);
    
public sealed record GetActiveLegalRepresentativeIdAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while retrieving legal representative ID.", Details, Exception);
    
public sealed record UpdateLegalRepresentativeAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while updating legal representative.", Details, Exception);
    
public sealed record InsertLegalRepresentativeAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while inserting legal representative.", Details, Exception);
    
public sealed record GetActiveBankAccountIdAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while retrieving the active bank account.", Details, Exception);
    
public sealed record InsertBankAccountAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while inserting the bank account.", Details, Exception);

public sealed record UpdateBankAccountAsyncError(string? Details = null, Exception? Exception = null)
    : CompanyError("An unexpected error occurred while updating the bank account.", Details, Exception);