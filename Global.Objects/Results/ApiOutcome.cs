namespace Global.Objects.Results;

/// <summary>
/// Represents the outcome of an API operation with HTTP status codes.
/// Non-generic version for API responses that don't return a value.
/// </summary>
public class ApiOutcome
{
    private readonly Exception? _exception;
    private readonly string _message;
    private readonly ApiResultType _apiResultType;

    protected ApiOutcome(ApiResultType apiResultType, string message = null!, Exception? exception = null)
    {
        _apiResultType = apiResultType;
        _message = message;
        _exception = exception;
    }

    /// <summary>
    /// The HTTP status code of this outcome
    /// </summary>
    public ApiResultType StatusCode => _apiResultType;

    /// <summary>
    /// The status code as an integer
    /// </summary>
    public int StatusCodeValue => (int)_apiResultType;

    /// <summary>
    /// Indicates if the status code represents a successful response (2xx range)
    /// </summary>
    public bool IsSuccess => StatusCodeValue is >= 200 and < 300;

    /// <summary>
    /// Indicates if the status code represents a failure response (non-2xx range)
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the exception if one was provided
    /// </summary>
    public Exception? Exception => _exception;

    /// <summary>
    /// Gets the message associated with this outcome
    /// </summary>
    public string Message => _message;

    /// <summary>
    /// Indicates if this outcome has an exception
    /// </summary>
    public bool HasException => _exception is not null;

    #region Success Status Methods

    /// <summary>
    /// Creates a successful outcome with OK (200) status
    /// </summary>
    public static ApiOutcome Ok(string message = null!) => new(ApiResultType.Ok, message);

    /// <summary>
    /// Creates a successful outcome with Created (201) status
    /// </summary>
    public static ApiOutcome Created(string message = null!) => new(ApiResultType.Created, message);

    /// <summary>
    /// Creates a successful outcome with Accepted (202) status
    /// </summary>
    public static ApiOutcome Accepted(string message = null!) => new(ApiResultType.Accepted, message);

    /// <summary>
    /// Creates a successful outcome with NoContent (204) status.
    /// Message is ignored for this status code.
    /// </summary>
    public static ApiOutcome NoContent() => new(ApiResultType.NoContent);

    /// <summary>
    /// Creates a successful outcome with NonAuthoritativeInformation (203) status
    /// </summary>
    public static ApiOutcome NonAuthoritativeInformation(string message = null!) => 
        new(ApiResultType.NonAuthoritativeInformation, message);

    /// <summary>
    /// Creates a successful outcome with ResetContent (205) status
    /// </summary>
    public static ApiOutcome ResetContent(string message = null!) => new(ApiResultType.ResetContent, message);

    /// <summary>
    /// Creates a successful outcome with PartialContent (206) status
    /// </summary>
    public static ApiOutcome PartialContent(string message = null!) => new(ApiResultType.PartialContent, message);

    /// <summary>
    /// Creates a successful outcome with MultiStatus (207) status
    /// </summary>
    public static ApiOutcome MultiStatus(string message = null!) => new(ApiResultType.MultiStatus, message);

    /// <summary>
    /// Creates a successful outcome with AlreadyReported (208) status
    /// </summary>
    public static ApiOutcome AlreadyReported(string message = null!) => new(ApiResultType.AlreadyReported, message);

    /// <summary>
    /// Creates a successful outcome with ImUsed (226) status
    /// </summary>
    public static ApiOutcome ImUsed(string message = null!) => new ApiOutcome(ApiResultType.ImUsed, message);

    #endregion

    #region Error Status Methods

    /// <summary>
    /// Creates an error outcome with BadRequest (400) status
    /// </summary>
    public static ApiOutcome BadRequest(string message = null!, Exception? exception = null) => 
        new(ApiResultType.BadRequest, message, exception);

    /// <summary>
    /// Creates an error outcome with Unauthorized (401) status
    /// </summary>
    public static ApiOutcome Unauthorized(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Unauthorized, message, exception);

    /// <summary>
    /// Creates an error outcome with Forbidden (403) status
    /// </summary>
    public static ApiOutcome Forbidden(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Forbidden, message, exception);

    /// <summary>
    /// Creates an error outcome with NotFound (404) status
    /// </summary>
    public static ApiOutcome NotFound(string message = null!, Exception? exception = null) => 
        new(ApiResultType.NotFound, message, exception);

    /// <summary>
    /// Creates an error outcome with Conflict (409) status
    /// </summary>
    public static ApiOutcome Conflict(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Conflict, message, exception);

    /// <summary>
    /// Creates an error outcome with UnprocessableEntity (422) status
    /// </summary>
    public static ApiOutcome UnprocessableEntity(string message = null!, Exception? exception = null) => 
        new(ApiResultType.UnprocessableEntity, message, exception);

    /// <summary>
    /// Creates an error outcome with TooManyRequests (429) status
    /// </summary>
    public static ApiOutcome TooManyRequests(string message = null!, Exception? exception = null) => 
        new(ApiResultType.TooManyRequests, message, exception);

    /// <summary>
    /// Creates an error outcome with InternalServerError (500) status
    /// </summary>
    public static ApiOutcome InternalServerError(string message = null!, Exception? exception = null) => 
        new(ApiResultType.InternalServerError, message, exception);

    /// <summary>
    /// Creates an error outcome with NotImplemented (501) status
    /// </summary>
    public static ApiOutcome NotImplemented(string message = null!, Exception? exception = null) => 
        new(ApiResultType.NotImplemented, message, exception);

    /// <summary>
    /// Creates an error outcome with ServiceUnavailable (503) status
    /// </summary>
    public static ApiOutcome ServiceUnavailable(string message = null!, Exception? exception = null) => 
        new(ApiResultType.ServiceUnavailable, message, exception);

    #endregion

    /// <summary>
    /// Creates an outcome with the specified status code
    /// </summary>
    public static ApiOutcome WithStatusCode(ApiResultType statusCode, string message = null!, Exception? exception = null)
    {
        return statusCode == ApiResultType.NoContent
            ? NoContent()
            : new ApiOutcome(statusCode, message, exception);
    }

    /// <summary>
    /// Create a generic ApiOutcome&lt;T&gt; from this ApiOutcome
    /// </summary>
    public static ApiOutcome<T> Ok<T>(T value, string message = null!) => ApiOutcome<T>.Ok(value, message);

    /// <summary>
    /// Create a generic ApiOutcome&lt;T&gt; from this ApiOutcome
    /// </summary>
    public static ApiOutcome<T> Created<T>(T value, string message = null!) => ApiOutcome<T>.Created(value, message);

    /// <summary>
    /// Create a generic ApiOutcome&lt;T&gt; from this ApiOutcome for BadRequest error
    /// </summary>
    public static ApiOutcome<T> BadRequest<T>(string message = null!, Exception? exception = null) => 
        ApiOutcome<T>.BadRequest(message, exception);

    /// <summary>
    /// Create a generic ApiOutcome&lt;T&gt; with a custom status code
    /// </summary>
    public static ApiOutcome<T> WithStatusCode<T>(ApiResultType statusCode, T value = default!, string message = null!, Exception? exception = null) => 
        ApiOutcome<T>.WithStatusCode(statusCode, value, message, exception);

    /// <summary>
    /// Creates an ApiOutcome based on an Exception, using InternalServerError status code
    /// </summary>
    public static ApiOutcome FromException(Exception exception, string? message = null) => 
        InternalServerError(message ?? exception.Message, exception);

    /// <summary>
    /// Creates an ApiOutcome based on an Exception, using the given status code
    /// </summary>
    public static ApiOutcome FromException(Exception exception, ApiResultType statusCode, string? message = null) => 
        WithStatusCode(statusCode, message ?? exception.Message, exception);

    /// <summary>
    /// Implicit conversion from exception to ApiOutcome
    /// </summary>
    public static implicit operator ApiOutcome(Exception exception) => FromException(exception);

    /// <summary>
    /// Returns a default message for the given status code
    /// </summary>
    private static string GetDefaultMessageForStatusCode(ApiResultType statusCode)
    {
        return statusCode switch
        {
            ApiResultType.Ok => "The request succeeded.",
            ApiResultType.Created => "The resource was created successfully.",
            ApiResultType.Accepted => "The request has been accepted for processing.",
            ApiResultType.NoContent => string.Empty // No content status should have no message
            ,
            ApiResultType.BadRequest => "The server cannot process the request due to a client error.",
            ApiResultType.Unauthorized => "Authentication is required to access this resource.",
            ApiResultType.Forbidden => "You don't have permission to access this resource.",
            ApiResultType.NotFound => "The requested resource could not be found.",
            ApiResultType.Conflict => "The request conflicts with the current state of the resource.",
            ApiResultType.UnprocessableEntity => "The request was well-formed but contains semantic errors.",
            ApiResultType.InternalServerError => "An unexpected error occurred on the server.",
            _ => $"Status code: {(int)statusCode}"
        };
    }

    /// <summary>
    /// Ensures the outcome has a specific status code and performs the action if it does
    /// </summary>
    public ApiOutcome OnStatusCode(ApiResultType statusCode, Action action)
    {
        if (_apiResultType == statusCode)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Ensures the outcome is successful and performs the action if it is
    /// </summary>
    public ApiOutcome OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Ensures the outcome is a failure and performs the action with the message if it is
    /// </summary>
    public ApiOutcome OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Message);
        }
        return this;
    }

    /// <summary>
    /// Ensures the outcome is a failure and performs the action with the exception if one exists
    /// </summary>
    public ApiOutcome OnException(Action<Exception> action)
    {
        if (HasException)
        {
            action(_exception!);
        }
        return this;
    }

    /// <summary>
    /// Maps a non-generic ApiOutcome to ApiOutcome&lt;T&gt; by providing a value for success cases
    /// </summary>
    public ApiOutcome<T> Map<T>(Func<T> valueFactory)
    {
        // Handle NoContent specially
        if (_apiResultType == ApiResultType.NoContent)
        {
            return ApiOutcome<T>.NoContent();
        }

        return IsSuccess 
            ? ApiOutcome<T>.WithStatusCode(_apiResultType, valueFactory(), _message) 
            : ApiOutcome<T>.WithStatusCode(_apiResultType, default!, _message, _exception);
    }

    /// <summary>
    /// Maps a non-generic ApiOutcome to ApiOutcome&lt;T&gt; by providing a constant value for success cases
    /// </summary>
    public ApiOutcome<T> Map<T>(T value)
    {
        // Handle NoContent specially
        if (_apiResultType == ApiResultType.NoContent)
        {
            return ApiOutcome<T>.NoContent();
        }

        return IsSuccess 
            ? ApiOutcome<T>.WithStatusCode(_apiResultType, value, _message) 
            : ApiOutcome<T>.WithStatusCode(_apiResultType, default!, _message, _exception);
    }

    /// <summary>
    /// Try to execute an action and return its outcome
    /// </summary>
    public static ApiOutcome Try(Action action)
    {
        try
        {
            action();
            return Ok();
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }
    
    /// <summary>
    /// Try to execute a function that returns an ApiOutcome and return its outcome
    /// </summary>
    public static ApiOutcome Try(Func<ApiOutcome> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }
    
    /// <summary>
    /// Try to execute a function that returns an ApiOutcome&lt;T&gt; and return its outcome
    /// </summary>
    public static ApiOutcome<T> Try<T>(Func<ApiOutcome<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return ApiOutcome<T>.FromException(ex);
        }
    }
    
    /// <summary>
    /// Try to execute a function that returns a value and wrap it in an ApiOutcome&lt;T&gt;
    /// </summary>
    public static ApiOutcome<T> Try<T>(Func<T> func)
    {
        try
        {
            return ApiOutcome<T>.Ok(func());
        }
        catch (Exception ex)
        {
            return ApiOutcome<T>.FromException(ex);
        }
    }
}

/// <summary>
/// Represents the outcome of an API operation with HTTP status codes.
/// Generic version for API responses that return a value on success.
/// </summary>
public class ApiOutcome<T> : ApiOutcome
{
    private readonly T _value;

    private ApiOutcome(ApiResultType apiResultType, T value = default!, string message = null!, Exception? exception = null)
        : base(apiResultType, message, exception)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value associated with this outcome.
    /// Throws InvalidOperationException for NoContent status or non-success statuses.
    /// </summary>
    public T Value => StatusCode == ApiResultType.NoContent 
        ? throw new InvalidOperationException("Cannot access Value when status is NoContent")
        : IsSuccess
            ? _value
            : throw new InvalidOperationException($"Cannot access Value when status is {StatusCode}");

    /// <summary>
    /// Safely tries to get the value without throwing an exception
    /// </summary>
    /// <returns>True if the value was retrieved successfully, false otherwise</returns>
    public bool TryGetValue(out T value)
    {
        if (IsSuccess && StatusCode is not ApiResultType.NoContent)
        {
            value = _value;
            return true;
        }
        
        value = default;
        return false;
    }

    #region Success Status Methods

    /// <summary>
    /// Creates a successful outcome with OK (200) status
    /// </summary>
    public static ApiOutcome<T> Ok(T value, string message = null!) => 
        new(ApiResultType.Ok, value, message);

    /// <summary>
    /// Creates a successful outcome with Created (201) status
    /// </summary>
    public static ApiOutcome<T> Created(T value, string message = null!) => 
        new(ApiResultType.Created, value, message);

    /// <summary>
    /// Creates a successful outcome with Accepted (202) status
    /// </summary>
    public static ApiOutcome<T> Accepted(T value, string message = null!) => 
        new(ApiResultType.Accepted, value, message);

    /// <summary>
    /// Creates a successful outcome with NoContent (204) status.
    /// Value and message are ignored for this status code.
    /// </summary>
    public new static ApiOutcome<T> NoContent() => 
        new(ApiResultType.NoContent);

    /// <summary>
    /// Creates a successful outcome with NonAuthoritativeInformation (203) status
    /// </summary>
    public static ApiOutcome<T> NonAuthoritativeInformation(T value, string message = null!) => 
        new(ApiResultType.NonAuthoritativeInformation, value, message);

    /// <summary>
    /// Creates a successful outcome with PartialContent (206) status
    /// </summary>
    public static ApiOutcome<T> PartialContent(T value, string message = null!) => 
        new(ApiResultType.PartialContent, value, message);

    #endregion

    #region Error Status Methods

    /// <summary>
    /// Creates an error outcome with BadRequest (400) status
    /// </summary>
    public new static ApiOutcome<T> BadRequest(string message = null!, Exception? exception = null) => 
        new(ApiResultType.BadRequest, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with Unauthorized (401) status
    /// </summary>
    public new static ApiOutcome<T> Unauthorized(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Unauthorized, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with Forbidden (403) status
    /// </summary>
    public new static ApiOutcome<T> Forbidden(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Forbidden, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with NotFound (404) status
    /// </summary>
    public new static ApiOutcome<T> NotFound(string message = null!, Exception? exception = null) => 
        new(ApiResultType.NotFound, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with Conflict (409) status
    /// </summary>
    public new static ApiOutcome<T> Conflict(string message = null!, Exception? exception = null) => 
        new(ApiResultType.Conflict, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with UnprocessableEntity (422) status
    /// </summary>
    public new static ApiOutcome<T> UnprocessableEntity(string message = null!, Exception? exception = null) => 
        new(ApiResultType.UnprocessableEntity, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with TooManyRequests (429) status
    /// </summary>
    public new static ApiOutcome<T> TooManyRequests(string message = null!, Exception? exception = null) => 
        new(ApiResultType.TooManyRequests, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with InternalServerError (500) status
    /// </summary>
    public new static ApiOutcome<T> InternalServerError(string message = null!, Exception? exception = null) => 
        new(ApiResultType.InternalServerError, default!, message, exception);

    /// <summary>
    /// Creates an error outcome with ServiceUnavailable (503) status
    /// </summary>
    public static ApiOutcome<T> ServiceUnavailable(string message = null!, Exception? exception = null) => 
        new(ApiResultType.ServiceUnavailable, default!, message, exception);

    #endregion

    /// <summary>
    /// Creates an outcome with the specified status code
    /// </summary>
    public static ApiOutcome<T> WithStatusCode(ApiResultType statusCode, T value = default!, string message = null!, Exception? exception = null)
    {
        return statusCode == ApiResultType.NoContent
            ? NoContent()
            : new ApiOutcome<T>(statusCode, value, message, exception);
    }

    /// <summary>
    /// Creates an ApiOutcome&lt;T&gt; based on an Exception, using InternalServerError status code
    /// </summary>
    public new static ApiOutcome<T> FromException(Exception exception, string? message = null) => 
        InternalServerError(message ?? exception.Message, exception);

    /// <summary>
    /// Creates an ApiOutcome&lt;T&gt; based on an Exception, using the given status code
    /// </summary>
    public new static ApiOutcome<T> FromException(Exception exception, ApiResultType statusCode, string? message = null) => 
        WithStatusCode(statusCode, default!, message ?? exception.Message, exception);

    /// <summary>
    /// Implicit conversion from value to successful outcome
    /// </summary>
    public static implicit operator ApiOutcome<T>(T value) => Ok(value);

    /// <summary>
    /// Implicit conversion from exception to failure outcome
    /// </summary>
    public static implicit operator ApiOutcome<T>(Exception exception) => FromException(exception);

    /// <summary>
    /// Ensures the outcome is successful and performs the action with the value if it is
    /// </summary>
    public ApiOutcome<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && StatusCode is not ApiResultType.NoContent)
        {
            action(_value);
        }
        return this;
    }

    /// <summary>
    /// Ensures the outcome has a specific status code and performs the action if it does
    /// </summary>
    public new ApiOutcome<T> OnStatusCode(ApiResultType statusCode, Action action)
    {
        base.OnStatusCode(statusCode, action);
        return this;
    }

    /// <summary>
    /// Ensures the outcome is a failure and performs the action with the message if it is
    /// </summary>
    public new ApiOutcome<T> OnFailure(Action<string> action)
    {
        base.OnFailure(action);
        return this;
    }

    /// <summary>
    /// Ensures the outcome is a failure and performs the action with the exception if one exists
    /// </summary>
    public new ApiOutcome<T> OnException(Action<Exception> action)
    {
        base.OnException(action);
        return this;
    }

    /// <summary>
    /// Maps a successful outcome to a new outcome with a different value type
    /// </summary>
    public ApiOutcome<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        // Handle NoContent specially
        if (StatusCode == ApiResultType.NoContent)
        {
            return ApiOutcome<TResult>.NoContent();
        }

        return IsSuccess 
            ? ApiOutcome<TResult>.WithStatusCode(StatusCode, mapper(_value), Message) 
            : ApiOutcome<TResult>.WithStatusCode(StatusCode, default, Message, Exception);
    }

    /// <summary>
    /// Binds the outcome to another outcome-returning function
    /// </summary>
    public ApiOutcome<TResult> Bind<TResult>(Func<T, ApiOutcome<TResult>> binder)
    {
        // Handle NoContent specially
        if (StatusCode == ApiResultType.NoContent)
        {
            return ApiOutcome<TResult>.NoContent();
        }

        return IsSuccess 
            ? binder(_value) 
            : ApiOutcome<TResult>.WithStatusCode(StatusCode, default!, Message, Exception);
    }

    /// <summary>
    /// Safely unwraps the outcome, either returning the value or the default value
    /// </summary>
    public T Unwrap(T defaultValue = default!)
    {
        return TryGetValue(out T value) ? value : defaultValue;
    }

    /// <summary>
    /// Safely unwraps the outcome with a function to handle the error case
    /// </summary>
    public T Unwrap(Func<string, T> errorHandler)
    {
        return TryGetValue(out T value) ? value : errorHandler(Message);
    }

    /// <summary>
    /// Safely unwraps the outcome with a function to handle the exception case
    /// </summary>
    public T UnwrapOrHandle(Func<Exception, T> exceptionHandler)
    {
        if (TryGetValue(out T value)) return value;
        return HasException ? exceptionHandler(Exception) : default;
    }

    /// <summary>
    /// Safely extracts the value or throws the original exception
    /// </summary>
    public T GetValueOrThrow()
    {
        if (IsFailure)
        {
            if (Exception is not null)
            {
                throw Exception;
            }
            throw new InvalidOperationException(Message);
        }
        
        if (StatusCode == ApiResultType.NoContent)
        {
            throw new InvalidOperationException("Cannot get value from NoContent response");
        }
        
        return _value;
    }
}

// Extension methods for working with async operations
public static class ApiOutcomeExtensions
{
    /// <summary>
    /// Converts a Task&lt;T&gt; to a Task&lt;ApiOutcome&lt;T&gt;&gt; that never throws but captures any exception
    /// </summary>
    public static async Task<ApiOutcome<T>> ToApiOutcome<T>(this Task<T> task)
    {
        try
        {
            T result = await task;
            return ApiOutcome.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiOutcome<T>.FromException(ex);
        }
    }

    /// <summary>
    /// Converts a Task to a Task&lt;ApiOutcome&gt; that never throws but captures any exception
    /// </summary>
    public static async Task<ApiOutcome> ToApiOutcome(this Task task)
    {
        try
        {
            await task;
            return ApiOutcome.Ok();
        }
        catch (Exception ex)
        {
            return ApiOutcome.FromException(ex);
        }
    }

    /// <summary>
    /// Converts an IEnumerable&lt;ApiOutcome&lt;T&gt;&gt; to an ApiOutcome&lt;IEnumerable&lt;T&gt;&gt;
    /// </summary>
    public static ApiOutcome<IEnumerable<T>> Combine<T>(this IEnumerable<ApiOutcome<T>> outcomes)
    {
        List<ApiOutcome<T>> outcomesList = outcomes.ToList();
        ApiOutcome<T>? failedOutcome = outcomesList.FirstOrDefault(o => o.IsFailure);
        
        if (failedOutcome is not null)
        {
            return ApiOutcome.WithStatusCode<IEnumerable<T>>(
                failedOutcome.StatusCode, 
                null!, 
                failedOutcome.Message, 
                failedOutcome.Exception);
        }
        
        // Skip NoContent results when extracting values
        List<T> values = outcomesList
            .Where(o => o.StatusCode is not ApiResultType.NoContent)
            .Select(o => o.Value)
            .ToList();
        
        return ApiOutcome.Ok<IEnumerable<T>>(values);
    }
}