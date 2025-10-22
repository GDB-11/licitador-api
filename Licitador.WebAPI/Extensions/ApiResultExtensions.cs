using Global.Objects.Results;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Extensions;

/// <summary>
/// Provides extension methods for converting ApiResult objects to IActionResult
/// with a standardized response format.
/// </summary>
public static class ApiResultExtensions
{
    /// <summary>
    /// Represents the standardized structure for API responses
    /// </summary>
    private sealed class ApiResponse
    {
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Converts an ApiResult to an IActionResult with a standardized response format.
    /// Response format: { "success": true/false, "message": "...", "data": ... }
    /// </summary>
    public static IActionResult ToActionResult(this ApiOutcome result)
    {
        // Special handling for NoContent responses
        if (result.StatusCode == ApiResultType.NoContent)
        {
            return new NoContentResult();
        }

        ApiResponse response = new()
        {
            Message = !string.IsNullOrEmpty(result.Message) ? result.Message : string.Empty
        };

        return new ObjectResult(response)
        {
            StatusCode = (int)result.StatusCode
        };
    }

    /// <summary>
    /// Converts an ApiResult{T} to an IActionResult with a standardized response format.
    /// Response format: { "success": true/false, "message": "...", "data": ... }
    /// </summary>
    public static IActionResult ToActionResult<T>(this ApiOutcome<T> result)
    {
        if (result.StatusCode == ApiResultType.NoContent)
        {
            return new NoContentResult();
        }

        ApiResponse response = new()
        {
            Message = !string.IsNullOrEmpty(result.Message) ? result.Message : string.Empty,
            Data = result.IsSuccess ? result.Value : null
        };

        return new ObjectResult(response)
        {
            StatusCode = (int)result.StatusCode
        };
    }
    
    /// <summary>
    /// Creates a standardized internal server error response with a user-friendly message.
    /// </summary>
    /// <param name="message">A user-friendly error message to display</param>
    /// <returns>An IActionResult with a 500 status code and the standardized error response format</returns>
    public static IActionResult InternalServerError(string message = "Unexpected server error.")
    {
        ApiResponse response = new()
        {
            Message = message,
            Data = null
        };

        return new ObjectResult(response)
        {
            StatusCode = (int)ApiResultType.InternalServerError
        };
    }
}