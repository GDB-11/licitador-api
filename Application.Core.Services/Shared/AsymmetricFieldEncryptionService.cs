using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Application.Core.DTOs.Encryption;
using Application.Core.Interfaces.Shared;
using Global.Attributes;
using Global.Helpers.Encryption;
using Global.Helpers.Functional;
using Global.Helpers.Reflection;
using Global.Objects.Encryption;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Security;
using Infrastructure.Core.Models.Security;

namespace Application.Core.Services.Shared;

public sealed class AsymmetricFieldEncryptionService : IAsymmetricFieldEncryption
{
    private readonly ITimeProvider _timeProvider;
    private readonly IKeyRepository _keyRepository;

    public AsymmetricFieldEncryptionService(ITimeProvider timeProvider, IKeyRepository keyRepository)
    {
        _timeProvider = timeProvider;
        _keyRepository = keyRepository;
    }

    public Task<Result<PublicKeyResponse, EncryptionError>> GenerateNewKeyPairAsync() =>
        RsaHelpers.GenerateKeyPair()
            .Map(keys => CreateKeyPair(keys.PublicKey, keys.PrivateKey))
            .BindAsync(PersistKeyPairAsync)
            .MapAsync(keyPair => new PublicKeyResponse
            {
                KeyPairId = keyPair.Id,
                PublicKey = keyPair.PublicKey
            });

    // ✨ Versión Funcional: DecryptRequestAsync
    public Task<Result<T, EncryptionError>> DecryptRequestAsync<T>(Guid keyPairId, T encryptedRequest) 
        where T : class =>
        GetActiveKeyPairAsync(keyPairId)
            .BindAsync(keyPair => ValidateKeyNotUsedAsync(keyPair))
            .BindAsync(keyPair => DecryptObjectAsync(keyPair, encryptedRequest))
            .TapAsync(async _ => await DeactivateKeyAsync(keyPairId));

    // 🔧 Métodos privados - Pipeline de GenerateNewKeyPairAsync

    private KeyPair CreateKeyPair(string publicKey, string privateKey) => new()
    {
        Id = Guid.CreateVersion7(),
        PublicKey = publicKey,
        PrivateKey = privateKey,
        IsActive = true,
        ExpiresAt = _timeProvider.UtcNow.AddMinutes(30)
    };

    private async Task<Result<KeyPair, EncryptionError>> PersistKeyPairAsync(KeyPair keyPair) =>
        (await _keyRepository.AddAsync(keyPair))
            .MapError(error => new KeyGenerationError(error.Message, error.Exception) as EncryptionError)
            .Map(_ => keyPair);

    // 🔧 Métodos privados - Pipeline de DecryptRequestAsync

    private async Task<Result<KeyPair, EncryptionError>> GetActiveKeyPairAsync(Guid keyPairId) =>
        (await _keyRepository.GetByIdAsync(keyPairId))
            .MapError(error => new KeyNotFoundError(error.Message, error.Exception) as EncryptionError);

    private Task<Result<KeyPair, EncryptionError>> ValidateKeyNotUsedAsync(KeyPair keyPair) =>
        Task.FromResult(
            keyPair.UsedAt.HasValue
                ? Result<KeyPair, EncryptionError>.Failure(
                    new KeyAlreadyUsedError("The key has already been used"))
                : Result<KeyPair, EncryptionError>.Success(keyPair)
        );

    private Task<Result<T, EncryptionError>> DecryptObjectAsync<T>(
        KeyPair keyPair, 
        T encryptedObject) where T : class =>
        RsaHelpers.ImportPrivateKey(keyPair.PrivateKey)
            .UsingAsync(rsa => Task.FromResult(DecryptAllProperties(rsa, encryptedObject)));

    private Result<T, EncryptionError> DecryptAllProperties<T>(RSA rsa, T encryptedObject) 
        where T : class =>
        ReflectionHelpers.CreateInstance<T>()
            .Bind(decryptedObject => ProcessAllProperties(rsa, encryptedObject, decryptedObject));

    private Result<T, EncryptionError> ProcessAllProperties<T>(
        RSA rsa,
        T source,
        T destination) where T : class
    {
        var properties = ReflectionHelpers.GetAllProperties<T>();
        
        foreach (PropertyInfo property in properties)
        {
            Result<Unit, EncryptionError> result = ProcessSingleProperty(rsa, property, source, destination);
            
            if (result.IsFailure)
                return Result<T, EncryptionError>.Failure(result.Error);
        }
        
        return Result<T, EncryptionError>.Success(destination);
    }

    private Result<Unit, EncryptionError> ProcessSingleProperty(
        RSA rsa,
        PropertyInfo property,
        object source,
        object destination)
    {
        bool isEncrypted = property.GetCustomAttribute<EncryptedFieldAttribute>() is not null;
        object? value = property.GetValue(source);

        return isEncrypted && value is not null
            ? DecryptAndSetProperty(rsa, property, value, destination)
            : ReflectionHelpers.CopyProperty(property, source, destination);
    }

    private Result<Unit, EncryptionError> DecryptAndSetProperty(
        RSA rsa,
        PropertyInfo property,
        object encryptedValue,
        object destination)
    {
        string? encryptedString = encryptedValue.ToString();

        if (string.IsNullOrEmpty(encryptedString))
            return ReflectionHelpers.SetPropertyValue(property, destination, string.Empty);

        return RsaHelpers.DecryptValue(rsa, encryptedString, property.Name)
            .Bind(decryptedValue => 
                ReflectionHelpers.SetPropertyValue(property, destination, decryptedValue)
            );
    }

    private async Task<Result<Unit, EncryptionError>> DeactivateKeyAsync(Guid keyPairId) =>
        (await _keyRepository.DeactivateKeyAsync(keyPairId))
            .MapError(EncryptionError (error) => new DecryptionError("Failed to deactivate key", Exception: error.Exception));
}