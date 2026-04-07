using System.Net;

namespace Amp.Core.Common.BaseClasses;

/// <summary>
/// Standardised API response envelope used across all AMP services.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public HttpStatusCode StatusCode { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null, string? correlationId = null) =>
        new() { Success = true, Data = data, Message = message, StatusCode = HttpStatusCode.OK, CorrelationId = correlationId };

    public static ApiResponse<T> Created(T data, string? correlationId = null) =>
        new() { Success = true, Data = data, StatusCode = HttpStatusCode.Created, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? correlationId = null) =>
        new() { Success = false, Message = message, StatusCode = statusCode, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? correlationId = null) =>
        new() { Success = false, Errors = errors.ToList().AsReadOnly(), StatusCode = statusCode, CorrelationId = correlationId };

    public static ApiResponse<T> NotFound(string message, string? correlationId = null) =>
        Fail(message, HttpStatusCode.NotFound, correlationId);

    public static ApiResponse<T> ServerError(string message, string? correlationId = null) =>
        Fail(message, HttpStatusCode.InternalServerError, correlationId);
}
