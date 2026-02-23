using Application.Core.DTOs.Consortium.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

public sealed class ConsortiumErrorMapper : IErrorHttpMapper<ConsortiumDomainError>
{
    public IActionResult MapToHttp(ConsortiumDomainError error) =>
        error switch
        {
            UserCompanyOwnershipError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            NoConsortiumCompaniesFound => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            NoConsortiumCompanyFound => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            ValidateUserCompanyOwnershipAsyncError validateError => new ObjectResult(new
            {
                message = validateError.Message,
                details = validateError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetAllCompaniesAsyncError getAllError => new ObjectResult(new
            {
                message = getAllError.Message,
                details = getAllError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ValidateCompanyConsortiumOwnershipAsyncError validateConsortiumError => new ObjectResult(new
            {
                message = validateConsortiumError.Message,
                details = validateConsortiumError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            InsertConsortiumCompanyAsyncError insertConsortiumError => new ObjectResult(new
            {
                message = insertConsortiumError.Message,
                details = insertConsortiumError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            InsertLegalRepresentativeAsyncError insertLegalRepError => new ObjectResult(new
            {
                message = insertLegalRepError.Message,
                details = insertLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateConsortiumCompanyAsyncError updateConsortiumError => new ObjectResult(new
            {
                message = updateConsortiumError.Message,
                details = updateConsortiumError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateLegalRepresentativeAsyncError updateLegalRepError => new ObjectResult(new
            {
                message = updateLegalRepError.Message,
                details = updateLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            
            DeleteConsortiumCompanyAsyncError deleteConsortiumError => new ObjectResult(new
            {
                message = deleteConsortiumError.Message,
                details = deleteConsortiumError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            DeleteLegalRepresentativeAsyncError deleteLegalRepError => new ObjectResult(new
            {
                message = deleteLegalRepError.Message,
                details = deleteLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}