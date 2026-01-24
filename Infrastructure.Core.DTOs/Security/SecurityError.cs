namespace Infrastructure.Core.DTOs.Security;

public abstract record SecurityError(string Message, string? Details = null, Exception? Exception = null);

public sealed record AddAsyncError(string? Details = null, Exception? Exception = null)
    : SecurityError("An unexpected error occurred while creating the encryption key.", Details, Exception);
    
public sealed record DeactivateKeyAsyncError(string? Details = null, Exception? Exception = null)
    : SecurityError("An unexpected error occurred while deactivating the encryption key.", Details, Exception);
    
public sealed record GetByIdAsyncError(string? Details = null, Exception? Exception = null)
    : SecurityError("An unexpected error occurred while obtaining the encryption key.", Details, Exception);