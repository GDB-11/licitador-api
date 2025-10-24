namespace Global.Objects.Company;

public abstract record CompanyError(string Message, string? Details = null, Exception? Exception = null);

public sealed record CompanyNotFoundError()
    : CompanyError("No company found for the user");

public sealed record InvalidUserIdError()
    : CompanyError("Invalid user ID in authentication token");

public sealed record CompanyRepositoryError(string Details, Exception? Exception = null)
    : CompanyError("An error occurred while retrieving company information", Details, Exception);

public sealed record CompanyUnauthorizedAccessError()
    : CompanyError("User does not have access to the specified company");

public sealed record CompanyValidationError(string Details)
    : CompanyError("Invalid company data provided", Details);