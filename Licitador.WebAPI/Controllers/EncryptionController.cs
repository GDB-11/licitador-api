using Application.Core.DTOs.Encryption;
using Application.Core.Interfaces.Shared;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[Route("api/[controller]")]
public sealed class EncryptionController : FunctionalController
{
    private readonly IEncryption _encryptionService;
    private readonly IErrorHttpMapper<ChaChaEncryptionError> _errorMapper;

    public EncryptionController(
        IEncryption encryptionService,
        IErrorHttpMapper<ChaChaEncryptionError> errorMapper,
        IResultLogger logger) 
        : base(logger)
    {
        _encryptionService = encryptionService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Encrypts the provided plaintext
    /// </summary>
    /// <param name="request">Encryption request containing plaintext</param>
    /// <returns>Encrypted ciphertext</returns>
    [HttpPost("encrypt")]
    [ProducesResponseType(typeof(EncryptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Encrypt([FromBody] EncryptRequest request) =>
        Execute(
            operation: () => _encryptionService.Encrypt(request.Plaintext)
                .Map(ciphertext => new EncryptResponse(ciphertext)),
            errorMapper: _errorMapper,
            operationName: nameof(Encrypt)
        );

    /// <summary>
    /// Decrypts the provided ciphertext
    /// </summary>
    /// <param name="request">Decryption request containing ciphertext</param>
    /// <returns>Decrypted plaintext</returns>
    [HttpPost("decrypt")]
    [ProducesResponseType(typeof(DecryptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Decrypt([FromBody] DecryptRequest request) =>
        Execute(
            operation: () => _encryptionService.Decrypt(request.Ciphertext)
                .Map(plaintext => new DecryptResponse(plaintext)),
            errorMapper: _errorMapper,
            operationName: nameof(Decrypt)
        );
}