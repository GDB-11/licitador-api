using Application.Core.DTOs.Company;
using Application.Core.Interfaces.Company;
using Global.Objects.Company;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CompanyController : FunctionalController
{
    private readonly ICompany _companyService;
    private readonly IErrorHttpMapper<CompanyError> _errorMapper;

    public CompanyController(
        ICompany companyService,
        IErrorHttpMapper<CompanyError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _companyService = companyService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Gets the first company associated with the authenticated user
    /// </summary>
    /// <returns>Company information including CompanyId and RazonSocial</returns>
    [HttpGet("my-company")]
    [ProducesResponseType(typeof(UserCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyCompany() =>
        ExecuteAuthenticatedAsync(
            operation: userId => _companyService.GetUserCompanyAsync(userId),
            errorMapper: _errorMapper,
            operationName: nameof(GetMyCompany)
        );

    /// <summary>
    /// Gets detailed information for a specific company if the authenticated user owns it
    /// </summary>
    /// <param name="companyId">The ID of the company to retrieve</param>
    /// <returns>Complete company information including legal representative and bank account</returns>
    [HttpGet("my-company-details/{companyId:guid}")]
    [ProducesResponseType(typeof(UserCompanyDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyCompanyDetails([FromRoute] Guid companyId) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _companyService.GetUserCompanyDetailsAsync(userId, companyId),
            errorMapper: _errorMapper,
            operationName: nameof(GetMyCompanyDetails)
        );

    /// <summary>
    /// Updates company details including legal representative and bank account
    /// </summary>
    /// <param name="request">The company details to update</param>
    /// <returns>NoContent on success</returns>
    [HttpPut("update-company-details")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateCompanyDetails([FromBody] UpdateCompanyDetailsRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userId => _companyService.UpdateCompanyDetailsAsync(userId, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateCompanyDetails),
            successMapper: _ => NoContent()
        );
}