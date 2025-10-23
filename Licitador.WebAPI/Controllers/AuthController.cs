using Application.Core.Interfaces.Shared;
using Global.Objects.Encryption;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController// : FunctionalController
{
    /*private readonly IEncryption _encryptionService;
    private readonly IErrorHttpMapper<ChaChaEncryptionError> _errorMapper;

    public AuthController(
        IEncryption encryptionService,
        IErrorHttpMapper<ChaChaEncryptionError> errorMapper,
        IResultLogger logger) 
        : base(logger)
    {
        _encryptionService = encryptionService;
        _errorMapper = errorMapper;
    }*/
    
    /*[AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        return HandleOperationAsync(
            () => _account.LoginAsync(request),
            "There was an error with the login request.");
    }*/
}