namespace Global.Objects.Encryption;

public abstract record DeterministicEncryptionError(string Message, Exception? Exception = null);

public sealed record DeterministicEncryptError(string Message, Exception? Exception = null)
    : DeterministicEncryptionError(Message, Exception);

public sealed record DeterministicDecryptError(string Message, Exception? Exception = null)
    : DeterministicEncryptionError(Message, Exception);

public sealed record InvalidKeyConfigurationError(string Message, Exception? Exception = null)
    : DeterministicEncryptionError(Message, Exception);

public sealed record AuthenticationFailedError(string Message, Exception? Exception = null)
    : DeterministicEncryptionError(Message, Exception);