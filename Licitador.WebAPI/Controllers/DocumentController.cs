using Application.Core.DTOs.Document;
using Application.Core.Interfaces.Document;
using Global.Objects.Document;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DocumentController : FunctionalController
{
    private readonly IDocument _documentService;
    private readonly IErrorHttpMapper<DocumentError> _errorMapper;

    public DocumentController(
        IDocument documentService,
        IErrorHttpMapper<DocumentError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _documentService = documentService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Generates annexes document (DOCX) based on licitacion information and company data
    /// </summary>
    /// <param name="request">Licitacion and notification information</param>
    /// <returns>DOCX file containing Anexo 1, 2, and 3</returns>
    [HttpPost("generate-annexes")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GenerateAnnexes([FromBody] GenerateAnnexesRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _documentService.GenerateAnnexesAsync(userId, request),
            errorMapper: _errorMapper,
            operationName: nameof(GenerateAnnexes),
            successMapper: fileBytes => File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"Anexos_{request.LicitacionNumber}_{DateTime.UtcNow:yyyyMMdd}.docx")
        );
}