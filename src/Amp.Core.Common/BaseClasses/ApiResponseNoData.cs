using System.Net;

namespace Amp.Core.Common.BaseClasses;

/// <summary>Non-generic convenience overload for operations that return no data.</summary>
public class ApiResponse : ApiResponse<object?>
{
    public static ApiResponse OkNoData(string? message = null, string? correlationId = null) =>
        new() { Success = true, Message = message, StatusCode = HttpStatusCode.OK, CorrelationId = correlationId };
}
