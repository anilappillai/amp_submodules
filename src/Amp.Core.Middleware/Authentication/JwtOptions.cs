namespace Amp.Core.Middleware.Authentication;

/// <summary>
/// JWT settings read automatically from <c>IConfiguration</c> (appsettings.json or
/// AWS Secrets Manager) under the <c>Jwt</c> section.
///
/// appsettings.json example:
/// <code>
///   "Jwt": {
///     "Authority": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_abc123",
///     "Audience":  "amp-facebook-api",
///     "ClientId":  "6example1clientid"
///   }
/// </code>
///
/// AWS Secrets Manager equivalent keys (double-underscore = nested section):
///   Jwt__Authority, Jwt__Audience, Jwt__ClientId
/// </summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>
    /// OIDC authority URL. Used for JWKS discovery and issuer validation.
    /// For AWS Cognito: https://cognito-idp.{region}.amazonaws.com/{userPoolId}
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Expected audience claim. Typically the API resource identifier or Cognito app client ID.
    /// If not set, <see cref="ClientId"/> is used as a fallback.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Cognito app client ID (or equivalent IdP client ID).
    /// Used as the audience fallback when <see cref="Audience"/> is not set.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>When true, HTTPS is required on the authority metadata endpoint. Default: true.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Allowed clock skew for token expiry validation. Default: 1 minute.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Resolved audience: <see cref="Audience"/> if set, otherwise <see cref="ClientId"/>.</summary>
    internal string ResolvedAudience => string.IsNullOrWhiteSpace(Audience) ? ClientId : Audience;
}
