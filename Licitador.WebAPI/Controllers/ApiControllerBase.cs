using System.Security.Claims;
using Global.Objects.Results;
using Licitador.WebAPI.Extensions;
using Licitador.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> ApiOk<T>(T data, string message = "Operation completed successfully")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }
        
    protected ActionResult<ApiResponse<T>> ApiError<T>(string message, T data = default!, int statusCode = StatusCodes.Status400BadRequest)
    {
        ApiResponse<T> response = ApiResponse<T>.ErrorResponse(message, data);
        return StatusCode(statusCode, response);
    }

    private static IActionResult HandleOutcome(ApiOutcome outcome)
    {
        if (outcome.IsFailure)
        {
            // Write log or handle specific failures if needed
        }
        
        return outcome.ToActionResult();
    }

    private static IActionResult HandleOutcome<T>(ApiOutcome<T> outcome)
    {
        if (outcome.IsFailure)
        {
            // Write log or handle specific failures if needed
        }
        
        return outcome.ToActionResult();
    }

    private static IActionResult HandleException(string userMessage, Exception exception)
    {
        //Log exception
        
        return ApiResultExtensions.InternalServerError(userMessage);
    }
    
    private Guid? GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirstValue("uid");
        
        if (Guid.TryParse(userIdClaim, out Guid userId))
        {
            return userId;
        }
        
        return null;
    }

    private ApiOutcome<Guid> RequireCurrentUserId()
    {
        Guid? userId = GetCurrentUserId();
        
        return userId ?? ApiOutcome<Guid>.Unauthorized("User ID not found in token or invalid format.");
    }
    
    /// <summary>
    /// Executes a synchronous operation that does not require a user ID, handling errors uniformly.
    /// This method is suitable for operations that return a non-generic <see cref="ApiOutcome"/> and do not involve asynchronous work.
    /// </summary>
    /// <param name="operation">The synchronous operation to execute, returning an <see cref="ApiOutcome"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method invokes the provided operation and processes its result using <see cref="HandleOutcome"/>.
    /// Any unhandled exceptions are caught, logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected IActionResult HandleOperation(
        Func<ApiOutcome> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome result = operation();
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }
    
    /// <summary>
    /// Executes a synchronous operation that does not require a user ID, handling errors uniformly.
    /// This method is suitable for operations that return a generic <see cref="ApiOutcome{T}"/> and do not involve asynchronous work.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the <see cref="ApiOutcome{T}"/> returned by the operation.</typeparam>
    /// <param name="operation">The synchronous operation to execute, returning an <see cref="ApiOutcome{T}"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method invokes the provided operation and processes its result using <see cref="HandleOutcome"/>.
    /// Any unhandled exceptions are caught, logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected static IActionResult HandleOperation<T>(
        Func<ApiOutcome<T>> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome<T> result = operation();
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }
    
    /// <summary>
    /// Executes an asynchronous operation that does not require a user ID, handling errors uniformly.
    /// This method is suitable for operations like login or other actions that return a non-generic <see cref="ApiOutcome"/>.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute, returning an <see cref="ApiOutcome"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method invokes the provided operation and processes its result using <see cref="HandleOutcome"/>.
    /// Any unhandled exceptions are caught, logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected static async Task<IActionResult> HandleOperationAsync(
        Func<Task<ApiOutcome>> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome result = await operation();
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }
    
    /// <summary>
    /// Executes an asynchronous operation that does not require a user ID, handling errors uniformly.
    /// This method is suitable for operations that return a generic <see cref="ApiOutcome{T}"/>, such as retrieving data or complex responses.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the <see cref="ApiOutcome{T}"/> returned by the operation.</typeparam>
    /// <param name="operation">The asynchronous operation to execute, returning an <see cref="ApiOutcome{T}"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method invokes the provided operation and processes its result using <see cref="HandleOutcome"/>.
    /// Any unhandled exceptions are caught, logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected static async Task<IActionResult> HandleOperationAsync<T>(
        Func<Task<ApiOutcome<T>>> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome<T> result = await operation();
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }
    
    /// <summary>
    /// Executes an asynchronous operation that requires a user ID, handling authentication and errors uniformly.
    /// This overload is used for operations that return a non-generic <see cref="ApiOutcome"/>, typically for actions
    /// like deletions or updates that don’t return data.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute, taking a user ID as input and returning an <see cref="ApiOutcome"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method ensures the current user ID is retrieved and validated before invoking the provided operation.
    /// If the user ID cannot be obtained, it returns an appropriate failure response. Any unhandled exceptions are caught,
    /// logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected async Task<IActionResult> HandleUserOperationAsync(
        Func<Guid, Task<ApiOutcome>> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome<Guid> userId = RequireCurrentUserId();

            if (userId.IsFailure)
            {
                return HandleOutcome(userId);
            }

            ApiOutcome result = await operation(userId.Value);
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Executes an asynchronous operation that requires a user ID, handling authentication and errors uniformly.
    /// This overload is used for operations that return a generic <see cref="ApiOutcome{T}"/>, typically for actions
    /// that retrieve or produce data.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the <see cref="ApiOutcome{T}"/> returned by the operation.</typeparam>
    /// <param name="operation">The asynchronous operation to execute, taking a user ID as input and returning an <see cref="ApiOutcome{T}"/>.</param>
    /// <param name="errorMessage">The user-friendly error message to display if an unhandled exception occurs.</param>
    /// <returns>An <see cref="IActionResult"/> representing the outcome of the operation, such as success, failure, or an error response.</returns>
    /// <remarks>
    /// This method ensures the current user ID is retrieved and validated before invoking the provided operation.
    /// If the user ID cannot be obtained, it returns an appropriate failure response. Any unhandled exceptions are caught,
    /// logged via <see cref="HandleException"/>, and returned as a consistent error response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    protected async Task<IActionResult> HandleUserOperationAsync<T>(
        Func<Guid, Task<ApiOutcome<T>>> operation,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        try
        {
            ApiOutcome<Guid> userId = RequireCurrentUserId();

            if (userId.IsFailure)
            {
                return HandleOutcome(userId);
            }

            ApiOutcome<T> result = await operation(userId.Value);
            return HandleOutcome(result);
        }
        catch (Exception ex)
        {
            return HandleException(errorMessage, ex);
        }
    }
}