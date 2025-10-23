using Global.Helpers.Functional;
using Global.Objects.Results;

namespace Licitador.WebAPI.Logging;

/// <summary>
/// Abstraction for logging operations (to be replaced with actual logging library)
/// </summary>
public interface IResultLogger
{
    void LogSuccess<T>(string operation, T value);
    void LogError<TError>(string operation, TError error);
    Task LogSuccessAsync<T>(string operation, T value);
    Task LogErrorAsync<TError>(string operation, TError error);
}

/// <summary>
/// Simulated logger implementation (replace with actual logging library)
/// </summary>
public sealed class ConsoleResultLogger : IResultLogger
{
    public void LogSuccess<T>(string operation, T value)
    {
        // Simulated: Replace with actual logger (Serilog, NLog, etc.)
        Console.WriteLine($"[SUCCESS] {operation}: {value}");
    }

    public void LogError<TError>(string operation, TError error)
    {
        // Simulated: Replace with actual logger
        Console.WriteLine($"[ERROR] {operation}: {error}");
    }

    public Task LogSuccessAsync<T>(string operation, T value)
    {
        LogSuccess(operation, value);
        return Task.CompletedTask;
    }

    public Task LogErrorAsync<TError>(string operation, TError error)
    {
        LogError(operation, error);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Functional extensions for logging Results as side effects
/// </summary>
public static class ResultLoggingExtensions
{
    /// <summary>
    /// Logs the result using the Tap pattern (side effect at the edge)
    /// </summary>
    public static Result<T, TError> LogResult<T, TError>(
        this Result<T, TError> result,
        IResultLogger logger,
        string operation)
    {
        return result
            .Tap(value => logger.LogSuccess(operation, value))
            .Tap(
                onSuccess: value => { }, // Already logged above
                onFailure: error => logger.LogError(operation, error)
            );
    }

    /// <summary>
    /// Asynchronously logs the result using the TapAsync pattern
    /// </summary>
    public static async Task<Result<T, TError>> LogResultAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        IResultLogger logger,
        string operation)
    {
        Result<T, TError> result = await resultTask;
    
        if (result.IsSuccess)
            await logger.LogSuccessAsync(operation, result.Value);
        else
            await logger.LogErrorAsync(operation, result.Error);
    
        return result;
    }

    /// <summary>
    /// Extended Tap that accepts both success and failure actions
    /// </summary>
    private static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> onSuccess,
        Action<TError> onFailure)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onFailure(result.Error);

        return result;
    }
}