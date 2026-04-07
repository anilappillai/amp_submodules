namespace Amp.Core.Middleware.Extensions;

public sealed class AmpMiddlewareOptions
{
    public bool EnableHealthCheck { get; set; } = true;
    public bool EnableJwtAuthentication { get; set; } = false; // opt-in
}
