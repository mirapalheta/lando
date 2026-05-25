using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Lando.Alexa.Security.HMAC;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.Core.Security.HMAC.Tests;

public class HmacSignatureVerifierTests
{
    private const string Secret = "test-secret-please-rotate-this-in-real-deployments";
    private const string SampleBody = """{"directive":{"header":{"namespace":"Alexa.Discovery","name":"Discover"}}}""";

    private static readonly DateTimeOffset FixedNow = new(2026, 5, 21, 17, 0, 0, TimeSpan.Zero);

    private static HmacSignatureVerifier CreateVerifier(uint maxSkewSeconds = 300, string secret = Secret)
        => new(Options.Create(new HmacOptions { SharedSecret = secret, MaxClockSkewSeconds = maxSkewSeconds }),
               () => FixedNow);

    private static (HttpHeaders headers, byte[] body) BuildValidRequest(
        long? timestampOverride = null,
        string? versionOverride = null,
        string? bodyOverride = null,
        string? secretOverride = null,
        Func<string, string>? signaturePostProcess = null)
    {
        var ts = (timestampOverride ?? FixedNow.ToUnixTimeSeconds()).ToString();
        var body = Encoding.UTF8.GetBytes(bodyOverride ?? SampleBody);
        var payload = Encoding.UTF8.GetBytes($"{ts}.").Concat(body).ToArray();
        var key = Encoding.UTF8.GetBytes(secretOverride ?? Secret);
        var hex = Convert.ToHexString(HMACSHA256.HashData(key, payload)).ToLowerInvariant();
        var sig = $"{versionOverride ?? "v1"}={hex}";
        if (signaturePostProcess is not null)
            sig = signaturePostProcess(sig);

        var headers = new HttpHeadersCollection(new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Lando-Timestamp"] = [ts],
            ["X-Lando-Signature"] = [sig],
        });
        return (headers, body);
    }

    [Fact]
    public void Verify_ValidSignature_DoesNotThrow()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();

        Should.NotThrow(() => verifier.Validate(headers, body, default!));
    }

    [Fact]
    public void Verify_TamperedBody_ThrowsSignatureMismatch()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();

        // Flip one byte after signing — signature was over the unmodified body
        body[0] ^= 0x01;

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.SignatureMismatch);
    }

    [Fact]
    public void Verify_ExpiredTimestamp_ThrowsStale()
    {
        var verifier = CreateVerifier(maxSkewSeconds: 300);
        var staleTs = FixedNow.ToUnixTimeSeconds() - 301;
        var (headers, body) = BuildValidRequest(timestampOverride: staleTs);

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.Stale);
    }

    [Fact]
    public void Verify_FutureTimestampBeyondSkew_ThrowsStale()
    {
        // Symmetric: clock-skew check is on |drift|, so a too-future ts also fails
        var verifier = CreateVerifier(maxSkewSeconds: 300);
        var futureTs = FixedNow.ToUnixTimeSeconds() + 301;
        var (headers, body) = BuildValidRequest(timestampOverride: futureTs);

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.Stale);
    }

    [Fact]
    public void Verify_NegativeTimestamp_ThrowsStale()
    {
        // Defence-in-depth: an attacker who sends long.MinValue used to
        // overflow Math.Abs in older code. The bounded-range check rejects
        // negative timestamps outright before any arithmetic.
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest(timestampOverride: long.MinValue);

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.Stale);
    }

    [Fact]
    public void Verify_MissingTimestampHeader_ThrowsMissingHeaders()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Remove("X-Lando-Timestamp");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.MissingHeaders);
    }

    [Fact]
    public void Verify_MissingSignatureHeader_ThrowsMissingHeaders()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Remove("X-Lando-Signature");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.MissingHeaders);
    }

    [Fact]
    public void Verify_DuplicateTimestampHeader_ThrowsDuplicateHeaders()
    {
        // A misbehaving proxy doubling a header must not degrade into
        // ambiguity (which value did we actually sign?). We reject loudly.
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Add("X-Lando-Timestamp", FixedNow.ToUnixTimeSeconds().ToString());

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.DuplicateHeaders);
    }

    [Fact]
    public void Verify_DuplicateSignatureHeader_ThrowsDuplicateHeaders()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Add("X-Lando-Signature", "v1=deadbeef");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.DuplicateHeaders);
    }

    [Fact]
    public void Verify_MalformedTimestamp_Throws()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Remove("X-Lando-Timestamp");
        headers.Add("X-Lando-Timestamp", "not-a-number");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.MalformedTimestamp);
    }

    [Fact]
    public void Verify_MalformedSignature_NoVersionPrefix_Throws()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Remove("X-Lando-Signature");
        headers.Add("X-Lando-Signature", "abcdef1234567890");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.MalformedSignature);
    }

    [Fact]
    public void Verify_MalformedSignature_BadHex_Throws()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();
        headers.Remove("X-Lando-Signature");
        headers.Add("X-Lando-Signature", "v1=not-actually-hex");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.MalformedSignature);
    }

    [Fact]
    public void Verify_WrongVersionPrefix_ThrowsUnsupportedVersion()
    {
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest(versionOverride: "v2");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.UnsupportedVersion);
    }

    [Fact]
    public void Verify_SecretMismatch_ThrowsSignatureMismatch()
    {
        var verifier = CreateVerifier();
        // Sign with a different secret than the verifier holds
        var (headers, body) = BuildValidRequest(secretOverride: "a-different-secret");

        Should.Throw<HmacVerificationException>(() => verifier.Validate(headers, body, default!))
            .Reason.ShouldBe(HmacVerificationFailureReason.SignatureMismatch);
    }

    [Fact]
    public void Verify_EmptyBody_Works()
    {
        // The verifier must handle a zero-byte payload — there's no Alexa
        // directive that ships empty today, but the canonical form is
        // {timestamp}.{body}, which is well-defined for body of length 0.
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest(bodyOverride: "");

        body.ShouldBeEmpty();

        Should.NotThrow(() => verifier.Validate(headers, body, default!));
    }

    [Fact]
    public void Verify_UppercaseHex_Works()
    {
        // Convert.FromHexString is case-insensitive. Some signers emit upper-
        // case hex; the verifier must not care. (The version prefix lookup
        // IS case-sensitive — "v1" only — so we keep that lowercase.)
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest(signaturePostProcess: s =>
        {
            var parts = s.Split('=');
            return parts[0] + "=" + parts[1].ToUpperInvariant();
        });

        Should.NotThrow(() => verifier.Validate(headers, body, default!));
    }

    [Fact]
    public void Verify_WhitespaceAroundSignatureParts_IsTrimmed()
    {
        // Headers occasionally pick up whitespace from intermediaries. We
        // explicitly TrimEntries on the split — assert that.
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest(signaturePostProcess: s =>
        {
            var parts = s.Split('=');
            return $" {parts[0]} =   {parts[1]}  ";
        });

        Should.NotThrow(() => verifier.Validate(headers, body, default!));
    }

    [Fact]
    public void Verify_CaseInsensitiveHeaderNames_Works()
    {
        // HttpHeadersCollection is case-insensitive by design; this asserts
        // that nothing in the verifier accidentally re-canonicalises to
        // case-sensitive lookup.
        var verifier = CreateVerifier();
        var (headers, _) = BuildValidRequest();
        var ts = headers.GetValues("X-Lando-Timestamp").Single();
        var sig = headers.GetValues("X-Lando-Signature").Single();
        var caseShifted = new HttpHeadersCollection(new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-lando-timestamp"] = [ts],
            ["X-LANDO-SIGNATURE"] = [sig],
        });
        var (_, body) = BuildValidRequest();

        Should.NotThrow(() => verifier.Validate(caseShifted, body, default!));
    }

    [Fact]
    public void Verify_FromBufferedStream_MatchesDirectByteArray()
    {
        // Regression test for the original showstopper bug: an earlier
        // implementation wrapped the body stream in a CaptureReadStream but
        // then read from the inner stream, so the verifier ended up hashing
        // an empty byte[] and every request 401'd. This test exercises the
        // exact buffering pattern FunctionBase uses (CopyToAsync into a
        // MemoryStream, then AsSpan) and asserts the validator sees the
        // signed bytes.
        var verifier = CreateVerifier();
        var (headers, body) = BuildValidRequest();

        // Simulate the inbound HttpRequestData.Body — a non-seekable-ish source.
        using var source = new MemoryStream(body);
        using var buffered = new MemoryStream();
        source.CopyTo(buffered);

        Should.NotThrow(() => verifier.Validate(headers, buffered.AsSpan(), default!));
    }

    [Fact]
    public void Construct_EmptySecret_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new HmacSignatureVerifier(
            Options.Create(new HmacOptions { SharedSecret = "" }),
            () => FixedNow));
    }

    [Fact]
    public void Construct_WhitespaceSecret_Throws()
    {
        // Empty and whitespace-only strings both indicate "not configured" —
        // catching them at the verifier's constructor is a backstop behind
        // the AddOptions startup validation.
        Should.Throw<InvalidOperationException>(() => new HmacSignatureVerifier(
            Options.Create(new HmacOptions { SharedSecret = "   " }),
            () => FixedNow));
    }
}
