namespace Global.Objects.Errors;

public sealed record GenericError(
    string Message,
    string? Details = null,
    Exception? Exception = null);