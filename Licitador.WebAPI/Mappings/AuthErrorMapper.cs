using Global.Objects.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Mappings;

public sealed class AuthErrorMapper : IErrorHttpMapper<AuthError>
{
    public IActionResult MapToHttp(AuthError error) =>
        error switch
        {
            InvalidCredentialsError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            UserInactiveError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            JwtGenerationError jwt => new ObjectResult(new
            {
                error = "InternalServerError",
                message = jwt.Message,
                details = jwt.Details
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