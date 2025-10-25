namespace Global.Objects.Document;

public abstract record DocumentError(string Message, string? Details = null, Exception? Exception = null);

public sealed record DocumentNotFoundError() : DocumentError("Document not found");

public sealed record DocumentGenerationError(string Details, Exception? Exception = null)
    : DocumentError("An error occurred while generating the document", Details, Exception);

public sealed record DocumentRepositoryError(string Details, Exception? Exception = null)
    : DocumentError("An error occurred while retrieving document data", Details, Exception);

public sealed record DocumentCompanyNotFoundError()
    : DocumentError("Company not found for the authenticated user");

public sealed record DocumentValidationError(string Details)
    : DocumentError("Document validation failed", Details);