using Global.Objects.Company;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

public sealed class CompanyErrorMapper : IErrorHttpMapper<CompanyError>
{
    public IActionResult MapToHttp(CompanyError error) =>
        error switch
        {
            CompanyNotFoundError => new NotFoundObjectResult(new
            {
                error = "NotFound",
                message = error.Message
            }),

            InvalidUserIdError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            CompanyUnauthorizedAccessError => new ObjectResult(new
            {
                error = "Forbidden",
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            CompanyValidationError validation => new BadRequestObjectResult(new
            {
                error = "ValidationError",
                message = validation.Message,
                details = validation.Details
            }),

            CompanyRepositoryError repository => new ObjectResult(new
            {
                error = "InternalServerError",
                message = repository.Message,
                details = repository.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                error = "InternalServerError",
                message = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}