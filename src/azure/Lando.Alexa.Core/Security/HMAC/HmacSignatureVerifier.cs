using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.Security.HMAC;

using static Constants.Hmac.Headers;

/// <summary>
/// Verifies HMAC signatures produced by the AWS Lambda proxy
/// (<c>signRequest</c> in <c>src/aws/lando-alexa-smart-home/src/hmac.ts</c>).
/// </summary>
/// <remarks>
/// <para>
/// The verifier is intentionally framework-agnostic: it takes a header
/// collection and a span of raw body bytes, so it can be unit-tested without
/// spinning up the Functions worker. Callers (typically
/// <c>FunctionBase&lt;TRequest,TResponse&gt;</c> in the Function App)
/// are responsible for buffering the request body before invoking
/// <see cref="Validate"/>.
/// </para>
/// <para>
/// The signing scheme is versioned via the <c>X-Lando-Signature</c> header
/// prefix (<c>v1=&lt;hex&gt;</c>). Each version is bound to a concrete
/// <see cref="SignatureScheme"/> that owns both the hash algorithm and the
/// canonical form fed to HMAC. Adding a new version is a one-place change:
/// declare a new scheme and add it to <see cref="Schemes"/>. Doing so lets
/// us roll forward in lockstep with the Lambda without ambiguity about which
/// bytes the signature covered.
/// </para>
/// </remarks>
public sealed class HmacSignatureVerifier : IRequestValidator
{
    /// <summary>
    /// Registered signature schemes, keyed by the version label that appears
    /// in the <c>X-Lando-Signature</c> header (e.g. <c>"v1"</c>). Lookup is
    /// case-sensitive — version labels are part of the wire contract.
    /// </summary>
    private static readonly FrozenDictionary<string, SignatureScheme> Schemes =
        new Dictionary<string, SignatureScheme>(StringComparer.Ordinal)
        {
            ["v1"] = new V1Scheme(),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private readonly byte[] _sharedKey;
    private readonly long _maxClockSkewSeconds;
    private readonly Func<DateTimeOffset> _now;

    /// <summary>
    /// Constructs a verifier bound to the options-provided shared secret and
    /// the system clock.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="HmacOptions.SharedSecret"/> is null or whitespace.
    /// In production this is also enforced via <c>ValidateOnStart</c>, so this
    /// constructor-time check is a defence-in-depth backstop.
    /// </exception>
    public HmacSignatureVerifier(IOptions<HmacOptions> options)
        : this(options, () => DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Test seam — lets tests pin a deterministic clock. Not for production use.
    /// </summary>
    internal HmacSignatureVerifier(IOptions<HmacOptions> options, Func<DateTimeOffset> now)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SharedSecret))
        {
            throw new InvalidOperationException(
                $"{nameof(HmacOptions)}.{nameof(HmacOptions.SharedSecret)} must be configured before the verifier is used.");
        }

        _sharedKey = Encoding.UTF8.GetBytes(options.Value.SharedSecret);
        _maxClockSkewSeconds = options.Value.MaxClockSkewSeconds;
        _now = now;
    }

    /// <inheritdoc />
    /// <remarks>
    /// On failure throws <see cref="HmacVerificationException"/> (a
    /// <see cref="LandoException"/> carrying <see cref="HttpStatusCode.Unauthorized"/>)
    /// with a <see cref="HmacVerificationFailureReason"/>; the return value
    /// is always <see langword="true"/>. <paramref name="correlationId"/>
    /// is currently unused — accepted for interface conformance and to
    /// reserve the slot for future structured logging from inside the
    /// verifier.
    /// </remarks>
    public bool Validate(HttpHeaders headers, ReadOnlySpan<byte> body, string correlationId)
    {
        ParseAndValidateHeaders(headers, out var timestamp, out var version, out var signature);
        VerifySignature(timestamp, body, version, signature);
        return true;
    }

    /// <summary>
    /// Resolves the scheme for <paramref name="version"/>, recomputes the HMAC
    /// over the canonical form, and constant-time compares against
    /// <paramref name="signature"/>.
    /// </summary>
    /// <exception cref="HmacVerificationException">
    /// Thrown with <see cref="HmacVerificationFailureReason.UnsupportedVersion"/>
    /// if no scheme is registered for <paramref name="version"/>, or with
    /// <see cref="HmacVerificationFailureReason.SignatureMismatch"/> if the
    /// recomputed HMAC differs from <paramref name="signature"/>.
    /// </exception>
    private void VerifySignature(long timestamp, ReadOnlySpan<byte> body, string version, ReadOnlySpan<byte> signature)
    {
        if (!Schemes.TryGetValue(version, out var scheme))
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.UnsupportedVersion,
                $"Signature version '{version}' is not supported.");
        }

        using var hmac = IncrementalHash.CreateHMAC(scheme.Algorithm, _sharedKey);
        scheme.AppendCanonicalForm(hmac, timestamp, body);

        Span<byte> computed = stackalloc byte[scheme.HashSizeBytes];
        hmac.GetHashAndReset(computed);

        if (!CryptographicOperations.FixedTimeEquals(signature, computed))
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.SignatureMismatch,
                "Signature did not match expected value.");
        }
    }

    /// <summary>
    /// Parses and validates the <c>X-Lando-Timestamp</c> and
    /// <c>X-Lando-Signature</c> headers, and asserts the timestamp falls
    /// within the configured clock-skew window.
    /// </summary>
    /// <remarks>
    /// Strictness is deliberate: each header must be present exactly once
    /// (so a misbehaving proxy doubling a header can't degrade into
    /// ambiguity), the timestamp must parse as Unix seconds in
    /// <see cref="CultureInfo.InvariantCulture"/>, and the signature must
    /// be exactly <c>&lt;version&gt;=&lt;hex&gt;</c>. Anything that doesn't
    /// match throws a typed <see cref="HmacVerificationException"/>; the
    /// signature math downstream relies on this method having normalised
    /// inputs.
    /// </remarks>
    /// <param name="headers">The HTTP headers from the inbound request.</param>
    /// <param name="timestamp">Parsed Unix-seconds timestamp.</param>
    /// <param name="version">Parsed signature version label (e.g. <c>"v1"</c>).</param>
    /// <param name="signature">Raw bytes decoded from the hex portion of the signature header.</param>
    /// <exception cref="HmacVerificationException">
    /// Thrown for any malformed, missing, duplicated, or stale input.
    /// </exception>
    private void ParseAndValidateHeaders(HttpHeaders headers, out long timestamp, out string version, out ReadOnlySpan<byte> signature)
    {
        if (!headers.TryGetValues(TimestampHeader, out var tsValues) ||
            !headers.TryGetValues(SignatureHeader, out var sigValues))
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.MissingHeaders,
                $"Required headers {TimestampHeader} and/or {SignatureHeader} were not present.");
        }

        // Materialise once so we don't enumerate the underlying header collection twice.
        var tsArray = tsValues as string[] ?? [.. tsValues];
        var sigArray = sigValues as string[] ?? [.. sigValues];

        if (tsArray.Length != 1 || sigArray.Length != 1)
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.DuplicateHeaders,
                $"Required headers {TimestampHeader} and/or {SignatureHeader} had multiple values.");
        }

        if (!long.TryParse(tsArray[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp))
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.MalformedTimestamp,
                $"Could not parse {TimestampHeader} as Unix seconds.");
        }

        // Reject timestamps outside the allowed window using a single bounded-range
        // expression. Doing the comparison this way avoids long-overflow hazards on
        // attacker-controlled input — Math.Abs(long.MinValue) throws.
        var now = _now().ToUnixTimeSeconds();
        if (timestamp < 0
            || timestamp > now + _maxClockSkewSeconds
            || timestamp < now - _maxClockSkewSeconds)
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.Stale,
                $"Timestamp {timestamp} is outside the allowed clock skew of {_maxClockSkewSeconds}s.");
        }

        if (sigArray[0].Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) is not { Length: 2 } parts)
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.MalformedSignature,
                $"{SignatureHeader} is not in the form '<version>=<hex>'.");
        }

        version = parts[0];
        try
        {
            // Convert.FromHexString is case-insensitive, so upper/lower/mixed hex all parse.
            signature = Convert.FromHexString(parts[1]);
        }
        catch (FormatException ex)
        {
            throw new HmacVerificationException(
                HmacVerificationFailureReason.MalformedSignature,
                $"{SignatureHeader} hex payload is malformed.", ex);
        }
    }

    /// <summary>
    /// A versioned signing scheme — pairs a hash algorithm with the canonical
    /// bytes that get fed into HMAC. Versioning the canonical form (and not
    /// just the algorithm) means future schemes can add length prefixes,
    /// additional headers, or different separators without ambiguity.
    /// </summary>
    private abstract class SignatureScheme
    {
        /// <summary>
        /// Hash algorithm used by this scheme's HMAC.
        /// </summary>
        public abstract HashAlgorithmName Algorithm { get; }

        /// <summary>
        /// Output size of <see cref="Algorithm"/> in bytes.
        /// </summary>
        public abstract int HashSizeBytes { get; }

        /// <summary>
        /// Appends the canonical bytes that <see cref="Algorithm"/> should
        /// MAC over for <paramref name="timestamp"/> and
        /// <paramref name="body"/>. Implementations must produce byte-identical
        /// output to the corresponding signer on the Lambda side.
        /// </summary>
        public abstract void AppendCanonicalForm(IncrementalHash hmac, long timestamp, ReadOnlySpan<byte> body);
    }

    /// <summary>
    /// v1 scheme: HMAC-SHA256 over UTF-8(<c>"{timestamp}."</c>) followed by
    /// the raw body bytes. Mirrors <c>signRequest</c> in
    /// <c>src/aws/lando-alexa-smart-home/src/hmac.ts</c>.
    /// </summary>
    private sealed class V1Scheme : SignatureScheme
    {
        public override HashAlgorithmName Algorithm => HashAlgorithmName.SHA256;
        public override int HashSizeBytes => 32; // SHA-256 output

        public override void AppendCanonicalForm(IncrementalHash hmac, long timestamp, ReadOnlySpan<byte> body)
        {
            // long.MaxValue is 19 digits; "+1" for the '.' separator; pad for safety.
            Span<byte> prefix = stackalloc byte[24];
            var len = Encoding.UTF8.GetBytes($"{timestamp}.", prefix);
            hmac.AppendData(prefix[..len]);
            hmac.AppendData(body);
        }
    }
}
