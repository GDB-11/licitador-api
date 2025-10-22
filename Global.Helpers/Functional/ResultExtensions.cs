using Global.Objects.Results;

namespace Global.Helpers.Functional;

public static class ResultExtensions
{
    /// <summary>
    /// Executes asynchronously code that may throw exceptions and converts it to a Result
    /// </summary>
    public static async Task<Result<T, string>> TryAsync<T>(
        Func<Task<T>> operation, 
        string errorMessage)
    {
        try
        {
            T result = await operation();
            return Result<T, string>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T, string>.Failure($"{errorMessage} Details: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Executes code that may throw exceptions and converts it to a Result
    /// </summary>
    public static Result<T, string> Try<T>(
        Func<T> operation, 
        string errorMessage)
    {
        try
        {
            T result = operation();
            return Result<T, string>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T, string>.Failure($"{errorMessage} Details: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a condition and returns Result
    /// </summary>
    public static Result<T, string> Ensure<T>(
        this Result<T, string> result,
        Func<T, bool> predicate,
        string errorMessage)
    {
        return result.IsSuccess && predicate(result.Value)
            ? result
            : Result<T, string>.Failure(errorMessage);
    }

    /// <summary>
    /// Executes a side effect action without changing the result
    /// </summary>
    public static async Task<Result<T, string>> TapAsync<T>(
        this Result<T, string> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        
        return result;
    }
    
    /// <summary>
    /// Converts a nullable value to a Result object
    /// </summary>
    public static Result<T, string> ToResult<T>(
        this T? value, 
        string errorMessage) where T : class
    {
        return value is not null
            ? Result<T, string>.Success(value)
            : Result<T, string>.Failure(errorMessage);
    }
    
    public static Result<T, TError> EnsureNotNull<T, TError>(
        this Result<T?, TError> result,
        TError errorWhenNull) where T : class
    {
        return result.Bind(value =>
            value is not null
                ? Result<T, TError>.Success(value)
                : Result<T, TError>.Failure(errorWhenNull)
        );
    }
    
    /// <summary>
    /// Asynchronously converts a nullable value to a Result object
    /// </summary>
    public static Task<Result<T, TError>> EnsureNotNullAsync<T, TError>(
        this Task<Result<T?, TError>> result,
        TError errorWhenNull) where T : class
    {
        return result.BindAsync(value =>
            value is not null
                ? Task.FromResult(Result<T, TError>.Success(value))
                : Task.FromResult(Result<T, TError>.Failure(errorWhenNull))
        );
    }
    
    /// <summary>
    /// Runs a side-effect asynchronously without changing the result
    /// </summary>
    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> action)
    {
        Result<T, TError> result = await resultTask;
    
        if (result.IsSuccess)
            await action(result.Value);
    
        return result;
    }

    /// <summary>
    /// Execute a side-effect without changing the result
    /// </summary>
    public static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
    
        return result;
    }
    
    /// <summary>
    /// Asynchronously executes an operation on an IDisposable resource and guarantees its disposition
    /// Similar to the "bracket" pattern in Haskell or "use" in F#
    /// </summary>
    public static async Task<Result<TResult, TError>> UsingAsync<TResource, TResult, TError>(
        this Result<TResource, TError> resource,
        Func<TResource, Task<Result<TResult, TError>>> operation) 
        where TResource : IDisposable
    {
        if (resource.IsFailure)
            return Result<TResult, TError>.Failure(resource.Error);

        try
        {
            return await operation(resource.Value);
        }
        finally
        {
            resource.Value.Dispose();
        }
    }

    /// <summary>
    /// Executes an operation on an IDisposable resource and guarantees its disposition
    /// Similar to the "bracket" pattern in Haskell or "use" in F#
    /// </summary>
    public static Result<TResult, TError> Using<TResource, TResult, TError>(
        this Result<TResource, TError> resource,
        Func<TResource, Result<TResult, TError>> operation) 
        where TResource : IDisposable
    {
        if (resource.IsFailure)
            return Result<TResult, TError>.Failure(resource.Error);

        try
        {
            return operation(resource.Value);
        }
        finally
        {
            resource.Value.Dispose();
        }
    }
}