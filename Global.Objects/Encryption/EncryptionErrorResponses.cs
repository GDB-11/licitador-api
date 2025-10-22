namespace Global.Objects.Encryption;

public abstract record EncryptionError(string Message, Exception? Exception = null);

public sealed record KeyGenerationError(string Message, Exception? Exception = null) 
    : EncryptionError(Message, Exception);

public sealed record KeyNotFoundError(string Message, Exception? Exception = null) 
    : EncryptionError(Message, Exception);

public sealed record KeyAlreadyUsedError(string Message) 
    : EncryptionError(Message);

public sealed record DecryptionError(string Message, string? FieldName = null, Exception? Exception = null) 
    : EncryptionError($"{Message}{(FieldName is not null ? $" (Field: '{FieldName}')" : "")}", Exception);