namespace Shared.Models;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int StatusCode { get; set; }
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ErrorResponse Create(int statusCode, string message, string? detail = null, string? traceId = null)
    {
        return new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Detail = detail,
            TraceId = traceId
        };
    }

    public static ErrorResponse ValidationError(Dictionary<string, string[]> errors, string? traceId = null)
    {
        return new ErrorResponse
        {
            StatusCode = 400,
            Message = "Validation failed",
            Errors = errors,
            TraceId = traceId
        };
    }

    public static ErrorResponse NotFound(string resource, string? traceId = null)
    {
        return new ErrorResponse
        {
            StatusCode = 404,
            Message = $"{resource} not found",
            TraceId = traceId
        };
    }

    public static ErrorResponse Conflict(string message, string? traceId = null)
    {
        return new ErrorResponse
        {
            StatusCode = 409,
            Message = message,
            TraceId = traceId
        };
    }

    public static ErrorResponse InternalServerError(string? detail = null, string? traceId = null)
    {
        return new ErrorResponse
        {
            StatusCode = 500,
            Message = "An internal server error occurred",
            Detail = detail,
            TraceId = traceId
        };
    }
}
