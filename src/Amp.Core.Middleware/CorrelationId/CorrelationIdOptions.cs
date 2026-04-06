namespace Amp.Core.Middleware.CorrelationId;

/// <summary>Configuration options for <see cref="CorrelationIdMiddleware"/>.</summary>
public sealed class CorrelationIdOptions
{
    /// <summary>
    /// HTTP header name used to read/write the correlation ID.
    /// Default: <c>X-Correlation-Id</c>.
    /// </summary>
    public string HeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// When true the correlation ID is echoed back in the response header.
    /// Default: <c>true</c>.
    /// </summary>
    public bool IncludeInResponse { get; set; } = true;

    /// <summary>
    /// When true a new GUID is generated if no correlation ID is present in the request.
    /// Default: <c>true</c>.
    /// </summary>
    public bool GenerateIfMissing { get; set; } = true;

    /// <summary>
    /// Pushes the correlation ID to Serilog's log context so it appears in every
    /// log entry for the request duration.
    /// Default: <c>true</c>.
    /// </summary>
    public bool PushToLogContext { get; set; } = true;
}
