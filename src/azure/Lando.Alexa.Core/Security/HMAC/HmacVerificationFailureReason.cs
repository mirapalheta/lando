namespace Lando.Alexa.Security.HMAC;

/// <summary>
/// Structured reason an HMAC verification failed. Useful for telemetry,
/// metrics, and asserting in tests. Never sent to the client — see
/// <see cref="HmacVerificationException"/>.
/// </summary>
public enum HmacVerificationFailureReason
{
    /// <summary>
    /// One of the required HMAC headers was absent.
    /// </summary>
    MissingHeaders,

    /// <summary>
    /// A required HMAC header was present more than once.
    /// </summary>
    DuplicateHeaders,

    /// <summary>
    /// The <c>X-Lando-Timestamp</c> header could not be parsed as a
    /// Unix-seconds integer.
    /// </summary>
    MalformedTimestamp,

    /// <summary>
    /// The <c>X-Lando-Signature</c> header was not in the expected
    /// <c>&lt;version&gt;=&lt;hex&gt;</c> form, or the hex payload was
    /// not valid hexadecimal.
    /// </summary>
    MalformedSignature,

    /// <summary>
    /// The signature version label (e.g. <c>"v1"</c>) does not correspond to
    /// any scheme registered by the verifier.
    /// </summary>
    UnsupportedVersion,

    /// <summary>
    /// The timestamp falls outside the configured clock-skew window — the
    /// request is either too old (potential replay) or too far in the future.
    /// </summary>
    Stale,

    /// <summary>
    /// All inputs were well-formed but the recomputed HMAC did not equal the
    /// supplied signature. The request was tampered with, signed against a
    /// different shared secret, or the signed canonical form on the sender
    /// side does not match what the verifier expects.
    /// </summary>
    SignatureMismatch,
}
