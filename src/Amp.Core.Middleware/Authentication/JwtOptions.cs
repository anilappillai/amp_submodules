namespace Amp.Core.Middleware.Authentication;

/// <summary>
/// JWT validation settings loaded from AWS Secrets Manager at startup.
/// No value is hardcoded — all fields must be present in the secret bundle.
///
/// Secret keys expected in Secrets Manager:
///   Jwt__Issuer      e.g. https://cognito-idp.us-east-1.amazonaws.com/us-east-1_abc123
///   Jwt__Audience    e.g. amp-facebook-api
///   Jwt__Authority   e.g. https://cognito-idp.us-east-1.amazonaws.com/us-east-1_abc123
/// </summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Token issuer (e.g. AWS Cognito User Pool URL).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Expected audience claim value.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>OIDC authority URL used to fetch JWKS for signature validation.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>When true, the token lifetime is validated.</summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>Allowed clock skew for token expiry validation.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>When true, HTTPS is required on the authority metadata endpoint.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
