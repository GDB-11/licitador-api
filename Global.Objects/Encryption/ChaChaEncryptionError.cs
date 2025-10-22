namespace Global.Objects.Encryption;

public abstract record ChaChaEncryptionError(string Message, Exception? Exception = null);

public sealed record ChaChaEncryptError(string Message, Exception? Exception = null)
    : ChaChaEncryptionError(Message, Exception);

public sealed record ChaChaDecryptError(string Message, Exception? Exception = null)
    : ChaChaEncryptionError(Message, Exception);

public sealed record InvalidKeyError(string Message, Exception? Exception = null)
    : ChaChaEncryptionError(Message, Exception);