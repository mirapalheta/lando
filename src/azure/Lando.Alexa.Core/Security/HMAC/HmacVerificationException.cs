using System;
using System.Net;

namespace Lando.Alexa.Security.HMAC;

/// <summary>
/// Raised by <see cref="HmacSignatureVerifier"/> when an inbound request
/// fails HMAC authentication.
/// </summary>
/// <remarks>
/// The structured <see cref="Reason"/> is for telemetry and is intentionally
/// not surfaced in the HTTP response body — the function returns
/// <see cref="HttpStatusCode.Unauthorized"/> with no detail, so a probing
/// client cannot use error specificity to refine attacks.
/// </remarks>
public sealed class HmacVerificationException(
    HmacVerificationFailureReason reason,
    string message = "HMAC verification failed",
    Exception? innerException = null)
    : LandoException(HttpStatusCode.Unauthorized, message, innerException)
{
    /// <summary>
    /// The structured reason this verification failed. Used for logging,
    /// metrics, and tests; never sent to the client.
    /// </summary>
    public HmacVerificationFailureReason Reason { get; } = reason;
}
