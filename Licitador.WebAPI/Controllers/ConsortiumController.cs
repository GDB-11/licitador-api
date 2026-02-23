using Application.Core.DTOs.Consortium.Errors;
using Application.Core.DTOs.Consortium.Request;
using Application.Core.DTOs.Consortium.Response;
using Application.Core.Interfaces.Consortium;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ConsortiumController : FunctionalController
{
    private readonly IConsortium _consortium;
    private readonly IErrorHttpMapper<ConsortiumDomainError> _errorMapper;

    public ConsortiumController(IConsortium consortium,
        IErrorHttpMapper<ConsortiumDomainError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _consortium = consortium;
        _errorMapper = errorMapper;
    }
    
    /// <summary>
    /// Gets all consortium companies associated with a company
    /// </summary>
    /// <param name="companyId">The ID of the parent company</param>
    /// <returns>List of consortium companies</returns>
    [HttpGet("{companyId:guid}")]
    [ProducesResponseType(typeof(GetAllCompaniesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAllCompanies([FromRoute] Guid companyId) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _consortium.GetAllCompaniesAsync(
                new GetAllCompaniesRequest { CompanyId = companyId }, userId),
            errorMapper: _errorMapper,
            operationName: nameof(GetAllCompanies)
        );

    /// <summary>
    /// Gets detailed data for a specific consortium company
    /// </summary>
    /// <param name="companyId">The ID of the parent company</param>
    /// <param name="consortiumCompanyId">The ID of the consortium company</param>
    /// <returns>Consortium company data including legal representative</returns>
    [HttpGet("{companyId:guid}/{consortiumCompanyId:guid}")]
    [ProducesResponseType(typeof(GetCompanyDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetCompanyData(
        [FromRoute] Guid companyId,
        [FromRoute] Guid consortiumCompanyId) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _consortium.GetCompanyDataAsync(
                new GetCompanyDataRequest
                {
                    CompanyId = companyId,
                    ConsortiumCompanyId = consortiumCompanyId
                }, userId),
            errorMapper: _errorMapper,
            operationName: nameof(GetCompanyData)
        );

    /// <summary>
    /// Creates a new consortium company linked to the specified parent company
    /// </summary>
    /// <param name="request">Consortium company data including legal representative</param>
    /// <returns>NoContent on success</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> CreateConsortiumCompany([FromBody] CreateConsortiumCompanyRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _consortium.CreateConsortiumCompanyAsync(request, userId),
            errorMapper: _errorMapper,
            operationName: nameof(CreateConsortiumCompany),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Updates an existing consortium company and its legal representative
    /// </summary>
    /// <param name="request">Updated consortium company data</param>
    /// <returns>NoContent on success</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateConsortiumCompany([FromBody] UpdateConsortiumCompanyRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _consortium.UpdateConsortiumCompanyAsync(request, userId),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateConsortiumCompany),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Deletes a consortium company and its legal representative
    /// </summary>
    /// <param name="companyId">The ID of the parent company</param>
    /// <param name="consortiumCompanyId">The ID of the consortium company to delete</param>
    /// <returns>NoContent on success</returns>
    [HttpDelete("{companyId:guid}/{consortiumCompanyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> DeleteConsortiumCompany(
        [FromRoute] Guid companyId,
        [FromRoute] Guid consortiumCompanyId) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _consortium.DeleteConsortiumCompanyAsync(
                new DeleteConsortiumCompanyRequest
                {
                    CompanyId = companyId,
                    ConsortiumCompanyId = consortiumCompanyId
                }, userId),
            errorMapper: _errorMapper,
            operationName: nameof(DeleteConsortiumCompany),
            successMapper: _ => NoContent()
        );
}