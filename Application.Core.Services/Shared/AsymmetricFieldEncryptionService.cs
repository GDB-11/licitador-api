using System.Reflection;
using System.Security.Cryptography;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Encryption.Response;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Global.Attributes;
using Global.Helpers.Encryption;
using Global.Helpers.Reflection;
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

    public async Task<Result<PublicKeyResponse, EncryptionError>> GenerateNewKeyPairAsync() =>
        await RsaHelpers.GenerateKeyPair()
            .MapError(EncryptionError (ex) => new GenerateNewKeyPairAsyncError(ex.Message, ex))
            .Map(keys => new KeyPair
            {
                Id = Guid.CreateVersion7(),
                PublicKey = keys.PublicKey,
                PrivateKey = keys.PrivateKey,
                IsActive = true,
                ExpiresAt = _timeProvider.UtcNow.AddMinutes(30)
            })
            .BindAsync(async keyPair => 
                await _keyRepository.AddAsync(keyPair)
                    .MapErrorAsync(EncryptionError (secError) => new PersistKeyPairAsyncError(secError.Message, secError.Exception))
                    .MapAsync(_ => keyPair))
            .MapAsync(keyPair => new PublicKeyResponse
            {
                KeyPairId = keyPair.Id,
                PublicKey = keyPair.PublicKey
            });
    
    public async Task<Result<T, EncryptionError>> DecryptRequestAsync<T>(Guid keyPairId, T encryptedRequest) 
    where T : class =>
        await _keyRepository.GetByIdAsync(keyPairId)
            .MapErrorAsync(EncryptionError (error) => new GetKeyError(error.Details, error.Exception))
            .EnsureNotNullAsync(new KeyNotFoundError())
            .EnsureAsync(
                keyPair => !keyPair.UsedAt.HasValue, 
                new KeyAlreadyUsedError())
            .BindAsync(keyPair => 
                RsaHelpers.ImportPrivateKey(keyPair.PrivateKey)
                    .MapError(EncryptionError (ex) => new ImportPrivateKeyError())
                    .UsingAsync(rsa => Task.FromResult(DecryptAllProperties(rsa, encryptedRequest)))
            )
            .TapAsync(async _ => await _keyRepository.DeactivateKeyAsync(keyPairId));

    private static Result<T, EncryptionError> DecryptAllProperties<T>(RSA rsa, T encryptedObject) 
        where T : class =>
        ReflectionHelpers.CreateInstance<T>()
            .MapError(EncryptionError (ex) => new CreateInstanceError(ex))
            .Bind(decryptedObject => 
            {
                var properties = ReflectionHelpers.GetAllProperties<T>();
                
                foreach (var property in properties)
                {
                    bool isEncrypted = property.GetCustomAttribute<EncryptedFieldAttribute>() is not null;
                    object? value = property.GetValue(encryptedObject);
                    
                    var result = isEncrypted && value is not null
                        ? DecryptAndSetProperty(rsa, property, value, decryptedObject)
                        : ReflectionHelpers.CopyProperty(property, encryptedObject, decryptedObject)
                            .MapError(EncryptionError (ex) => new CopyPropertyError(ex));
                    
                    if (result.IsFailure)
                        return Result<T, EncryptionError>.Failure(result.Error);
                }
                
                return Result<T, EncryptionError>.Success(decryptedObject);
            });

    private static Result<Unit, EncryptionError> DecryptAndSetProperty(
        RSA rsa,
        PropertyInfo property,
        object encryptedValue,
        object destination)
    {
        string? encryptedString = encryptedValue.ToString();

        if (string.IsNullOrEmpty(encryptedString))
            return ReflectionHelpers.SetPropertyValue(property, destination, string.Empty)
                .MapError(EncryptionError (ex) => new SetPropertyValueError(ex));

        return RsaHelpers.DecryptValue(rsa, encryptedString, property.Name)
            .MapError(EncryptionError (ex) => new DecryptValueError(ex))
            .Bind(decryptedValue => 
                ReflectionHelpers.SetPropertyValue(property, destination, decryptedValue)
                    .MapError(EncryptionError (ex) => new SetPropertyValueError(ex)));
    }
}