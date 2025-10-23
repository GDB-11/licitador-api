using Global.Objects.Results;
using Licitador.WebAPI.Extensions;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

/// <summary>
/// Base controller with functional programming patterns
/// Handles Result -> HTTP conversion and logging as side effects
/// </summary>
[ApiController]
public abstract class FunctionalController : ControllerBase
{
    private readonly IResultLogger _logger;

    protected FunctionalController(IResultLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation and converts the Result to an HTTP response
    /// Logging happens as a side effect at the edge
    /// </summary>
    protected IActionResult Execute<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an async operation and converts the Result to an HTTP response
    /// </summary>
    protected async Task<IActionResult> ExecuteAsync<T, TError>(
        Func<Task<Result<T, TError>>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, IActionResult>? successMapper = null)
    {
        Result<T, TError> result = await operation();
        
        return result
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successMapper);
    }

    /// <summary>
    /// Executes an operation that returns NoContent on success
    /// </summary>
    protected IActionResult ExecuteNoContent<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToNoContentResult(errorMapper);
    }

    /// <summary>
    /// Executes an operation that returns Created (201) on success
    /// </summary>
    protected IActionResult ExecuteCreated<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        Func<T, string> locationFactory)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToCreatedResult(errorMapper, locationFactory);
    }

    /// <summary>
    /// Executes an operation with a custom success status code
    /// </summary>
    protected IActionResult ExecuteWithStatus<T, TError>(
        Func<Result<T, TError>> operation,
        IErrorHttpMapper<TError> errorMapper,
        string operationName,
        int successStatusCode)
    {
        return operation()
            .LogResult(_logger, operationName)
            .ToHttpResult(errorMapper, successStatusCode);
    }
}