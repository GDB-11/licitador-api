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
}