namespace Licitador.WebAPI.Models;

public sealed class ApiResponse<T>
{
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, T data = default!)
    {
        return new ApiResponse<T>
        {
            Message = message,
            Data = data
        };
    }
}