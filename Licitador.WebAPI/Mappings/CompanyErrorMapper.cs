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
                message = error.Message
            }),

            UserCompanyOwnershipError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            InvalidBase64ImageFormatError imageError => new BadRequestObjectResult(new
            {
                message = imageError.Message,
                details = imageError.Details
            }),

            GetUserFirstCompanyAsyncError getUserError => new ObjectResult(new
            {
                message = getUserError.Message,
                details = getUserError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ValidateUserCompanyOwnershipAsyncError validateError => new ObjectResult(new
            {
                message = validateError.Message,
                details = validateError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetCompanyDetailsAsyncError getDetailsError => new ObjectResult(new
            {
                message = getDetailsError.Message,
                details = getDetailsError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateCompanyAsyncError updateError => new ObjectResult(new
            {
                message = updateError.Message,
                details = updateError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetActiveLegalRepresentativeIdAsyncError getLegalRepError => new ObjectResult(new
            {
                message = getLegalRepError.Message,
                details = getLegalRepError.Details
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

            InsertLegalRepresentativeAsyncError insertLegalRepError => new ObjectResult(new
            {
                message = insertLegalRepError.Message,
                details = insertLegalRepError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetActiveBankAccountIdAsyncError getBankError => new ObjectResult(new
            {
                message = getBankError.Message,
                details = getBankError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            InsertBankAccountAsyncError insertBankError => new ObjectResult(new
            {
                message = insertBankError.Message,
                details = insertBankError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UpdateBankAccountAsyncError updateBankError => new ObjectResult(new
            {
                message = updateBankError.Message,
                details = updateBankError.Details
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