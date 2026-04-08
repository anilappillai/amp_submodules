using System.Net;

namespace Amp.Core.Common.BaseClasses;

/// <summary>Non-generic convenience overload for operations that return no data.</summary>
public class ApiResponse : ApiResponse<object?>
{
    /// <summary>Returns HTTP 200 OK with no data payload.</summary>
    public static ApiResponse OkNoData(string? message = null, string? correlationId = null) =>
        new() { Success = true, Message = message, StatusCode = HttpStatusCode.OK, CorrelationId = correlationId };

    /// <summary>
    /// Returns HTTP 204 No Content — the standard success response for DELETE and PUT operations
    /// that do not return a body.
    /// </summary>
    public static ApiResponse NoContent(string? correlationId = null) =>
        new() { Success = true, StatusCode = HttpStatusCode.NoContent, CorrelationId = correlationId };

    /// <summary>Returns HTTP 401 Unauthorized — authentication is required.</summary>
    public static ApiResponse Unauthorized(string message = "Authentication required.", string? correlationId = null) =>
        new() { Success = false, Message = message, StatusCode = HttpStatusCode.Unauthorized, CorrelationId = correlationId };

    /// <summary>Returns HTTP 403 Forbidden — authenticated but not permitted.</summary>
    public static ApiResponse Forbidden(string message = "Access denied.", string? correlationId = null) =>
        new() { Success = false, Message = message, StatusCode = HttpStatusCode.Forbidden, CorrelationId = correlationId };
}
