namespace Infrastructure.Core.DTOs.Account;

public abstract record UserError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetByEmailAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while retrieving the user with the email.", Details, Exception);
    
public sealed record GetByIdAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while retrieving the user by the id.", Details, Exception);
    
public sealed record GetByRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while retrieving the user by the refresh token.", Details, Exception);
    
public sealed record UpdateRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while updating the refresh token.", Details, Exception);
    
public sealed record ClearRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while deleting the refresh token.", Details, Exception);
    
public sealed record GetUserFirstCompanyAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while retrieving the user's first company.", Details, Exception);
    
public sealed record ValidateUserCompanyOwnershipAsyncError(string? Details = null, Exception? Exception = null)
    : UserError("An unexpected error occurred while validating the company ownership of the user.", Details, Exception);