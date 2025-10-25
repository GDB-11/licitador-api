using Global.Objects.Document;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

public sealed class DocumentErrorMapper : IErrorHttpMapper<DocumentError>
{
    public IActionResult MapToHttp(DocumentError error)
    {
        return error switch
        {
            DocumentNotFoundError => new NotFoundObjectResult(new
            {
                error = "NotFound",
                message = error.Message,
                details = error.Details
            }),

            DocumentCompanyNotFoundError => new NotFoundObjectResult(new
            {
                error = "CompanyNotFound",
                message = error.Message,
                details = error.Details
            }),

            DocumentValidationError => new BadRequestObjectResult(new
            {
                error = "ValidationError",
                message = error.Message,
                details = error.Details
            }),

            DocumentRepositoryError => new ObjectResult(new
            {
                error = "RepositoryError",
                message = error.Message,
                details = error.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            DocumentGenerationError => new ObjectResult(new
            {
                error = "GenerationError",
                message = error.Message,
                details = error.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                error = "UnknownError",
                message = error.Message,
                details = error.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}