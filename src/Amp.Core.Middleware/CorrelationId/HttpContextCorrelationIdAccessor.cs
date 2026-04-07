using Microsoft.AspNetCore.Http;

namespace Amp.Core.Middleware.CorrelationId;

internal sealed class HttpContextCorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    public string? CorrelationId =>
        accessor.HttpContext?.Items["CorrelationId"]?.ToString();
}
