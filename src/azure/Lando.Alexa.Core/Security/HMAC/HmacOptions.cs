namespace Lando.Alexa.Security.HMAC;

/// <summary>
/// Options bound from configuration section <see cref="SectionName"/>. In
/// production these are populated via a Key Vault reference; for local dev
/// use <c>local.settings.json</c> or <c>dotnet user-secrets</c>.
/// </summary>
/// <remarks>
/// <see cref="HmacSignatureVerifier"/> captures these values at construction
/// (it is registered as a singleton). Updating the bound configuration at
/// runtime will not affect already-resolved verifiers — secret rotation
/// requires a restart, which is acceptable here because all configuration
/// flows through Terraform-driven redeploys.
/// </remarks>
public sealed class HmacOptions
{
    /// <summary>
    /// Configuration section name (<c>"Hmac"</c>).
    /// </summary>
    public const string SectionName = "Hmac";

    /// <summary>
    /// Shared secret used to verify <c>X-Lando-Signature</c>. The same secret
    /// is held in AWS Secrets Manager and signed against by the Lambda proxy.
    /// </summary>
    public string SharedSecret { get; set; } = string.Empty;

    /// <summary>
    /// Maximum allowed difference (in seconds) between the
    /// <c>X-Lando-Timestamp</c> header and the verifier's clock. Defaults to
    /// 300s (5 minutes), matching Slack and Stripe's webhook replay windows.
    /// </summary>
    public uint MaxClockSkewSeconds { get; set; } = 300;
}
