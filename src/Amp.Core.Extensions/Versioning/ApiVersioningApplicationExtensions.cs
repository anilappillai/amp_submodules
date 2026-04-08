using Microsoft.AspNetCore.Builder;

namespace Amp.Core.Extensions.Versioning;

/// <summary>
/// Pipeline extension that adds the AMP API version middleware.
///
/// Call after <c>UseRouting()</c> and before <c>UseAuthentication()</c>:
/// <code>
///   app.UseRouting();
///   app.UseAmpCoreVersioning();   // stamps X-Api-Version on every response
///   app.UseAuthentication();
///   app.UseAuthorization();
///   app.MapControllers();
/// </code>
/// </summary>
public static class ApiVersioningApplicationExtensions
{
    public static IApplicationBuilder UseAmpCoreVersioning(this IApplicationBuilder app) =>
        app.UseMiddleware<ApiVersionHeaderMiddleware>();
}
