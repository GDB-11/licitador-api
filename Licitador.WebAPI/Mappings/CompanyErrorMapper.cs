using Application.Core.DTOs.Company.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

public sealed class CompanyErrorMapper : IErrorHttpMapper<CompanyDomainError>
{
    public IActionResult MapToHttp(CompanyDomainError error) =>
        error switch
        {
            CompanyNotFoundError => new NotFoundObjectResult(new
            {
                error = "NotFound",
                message = error.Message
            }),

            UserCompanyOwnershipError => new ObjectResult(new
            {
                error = "Forbidden",
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            InvalidBase64ImageFormatError imageError => new BadRequestObjectResult(new
            {
                error = "ValidationError",
                message = imageError.Message,
                details = imageError.Details
            }),

            GetUserFirstCompanyAsyncError getUserError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = getUserError.Message,
                details = getUserError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ValidateUserCompanyOwnershipAsyncError validateError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = validateError.Message,
                details = validateError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetCompanyDetailsAsyncError getDetailsError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = getDetailsError.Message,
                details = getDetailsError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateCompanyAsyncError updateError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = updateError.Message,
                details = updateError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetActiveLegalRepresentativeIdAsyncError getLegalRepError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = getLegalRepError.Message,
                details = getLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateLegalRepresentativeAsyncError updateLegalRepError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = updateLegalRepError.Message,
                details = updateLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            InsertLegalRepresentativeAsyncError insertLegalRepError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = insertLegalRepError.Message,
                details = insertLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetActiveBankAccountIdAsyncError getBankError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = getBankError.Message,
                details = getBankError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            InsertBankAccountAsyncError insertBankError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = insertBankError.Message,
                details = insertBankError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateBankAccountAsyncError updateBankError => new ObjectResult(new
            {
                error = "InternalServerError",
                message = updateBankError.Message,
                details = updateBankError.Details
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