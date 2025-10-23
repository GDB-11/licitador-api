using Global.Objects.Encryption;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

/// <summary>
/// Default implementation for ChaCha encryption errors
/// </summary>
public sealed class ChaChaEncryptionErrorMapper : IErrorHttpMapper<ChaChaEncryptionError>
{
    public IActionResult MapToHttp(ChaChaEncryptionError error) =>
        error switch
        {
            ChaChaEncryptError encryptError => new BadRequestObjectResult(new
            {
                Type = "EncryptionFailed",
                Message = encryptError.Message,
                Detail = encryptError.Exception?.Message
            }),
            
            ChaChaDecryptError decryptError => new BadRequestObjectResult(new
            {
                Type = "DecryptionFailed",
                Message = decryptError.Message,
                Detail = decryptError.Exception?.Message
            }),
            
            InvalidKeyError keyError => new UnprocessableEntityObjectResult(new
            {
                Type = "InvalidKey",
                Message = keyError.Message,
                Detail = keyError.Exception?.Message
            }),
            
            _ => new StatusCodeResult(500)
        };
}