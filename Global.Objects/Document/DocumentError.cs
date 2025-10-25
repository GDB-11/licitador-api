namespace Global.Objects.Document;

public abstract record DocumentError(string Message, string? Details = null, Exception? Exception = null);