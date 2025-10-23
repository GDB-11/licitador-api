using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

/// <summary>
/// Maps domain errors to HTTP responses
/// </summary>
public interface IErrorHttpMapper<in TError>
{
    IActionResult MapToHttp(TError error);
}