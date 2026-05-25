using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.HomeAssistant.Core.Security.Tests;

public class X509Certificate2ExtensionsTests
{
    // ── Cert factory ─────────────────────────────────────────────────────────

    private static X509Certificate2 CreateCert(
        string cn,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        string[]? sanDnsNames = null)
    {
        using var key = RSA.Create(2048);
        var req = new CertificateRequest($"CN={cn}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (sanDnsNames?.Length > 0)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var name in sanDnsNames)
                sanBuilder.AddDnsName(name);
            req.CertificateExtensions.Add(sanBuilder.Build());
        }

        return req.CreateSelfSigned(notBefore, notAfter);
    }

    private static X509Certificate2 ValidCert(string cn = "host.example.com", string[]? san = null) =>
        CreateCert(cn, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365), san);

    private static HttpRequestMessage RequestFor(string url) =>
        new(HttpMethod.Get, new Uri(url));

    // ── WebSocket variant (extension on X509Certificate?) ────────────────────

    [Fact]
    public void WebSocket_NullCertificate_ReturnsFalse()
    {
        X509Certificate? cert = null;

        cert.IsValid(ValidCert(), NullLogger.Instance).ShouldBeFalse();
    }

    [Fact]
    public void WebSocket_NonX509Certificate2_ReturnsFalse()
    {
        X509Certificate cert = new BareX509Certificate();

        cert.IsValid(ValidCert(), NullLogger.Instance).ShouldBeFalse();
    }

    [Fact]
    public void WebSocket_ValidCertificate_ReturnsTrue()
    {
        var cert = ValidCert("ws.example.com");
        X509Certificate asBase = cert;

        asBase.IsValid(cert, NullLogger.Instance).ShouldBeTrue();
    }

    // ── HTTP variant: null / thumbprint / validity ────────────────────────────

    [Fact]
    public void Http_NullCertificate_ReturnsFalse()
    {
        X509Certificate2? cert = null;

        cert.IsValid(RequestFor("https://host.example.com"), ValidCert(), NullLogger.Instance)
            .ShouldBeFalse();
    }

    [Fact]
    public void Http_ThumbprintMismatch_ReturnsFalse()
    {
        var serverCert = ValidCert("host.example.com");
        var differentCaCert = ValidCert("other.example.com");

        serverCert.IsValid(RequestFor("https://host.example.com"), differentCaCert, NullLogger.Instance)
            .ShouldBeFalse();
    }

    [Fact]
    public void Http_ExpiredCertificate_ReturnsFalse()
    {
        var cert = CreateCert("host.example.com",
            notBefore: DateTimeOffset.UtcNow.AddDays(-10),
            notAfter: DateTimeOffset.UtcNow.AddDays(-1));

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeFalse();
    }

    [Fact]
    public void Http_NotYetValidCertificate_ReturnsFalse()
    {
        var cert = CreateCert("host.example.com",
            notBefore: DateTimeOffset.UtcNow.AddDays(1),
            notAfter: DateTimeOffset.UtcNow.AddDays(365));

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeFalse();
    }

    // ── HTTP variant: message = null (WebSocket handshake bypass) ────────────

    [Fact]
    public void Http_NullMessage_SkipsHostnameCheck_ReturnsTrue()
    {
        var cert = ValidCert("host.example.com");

        cert.IsValid(message: null, cert, NullLogger.Instance).ShouldBeTrue();
    }

    // ── HTTP variant: hostname matching ──────────────────────────────────────

    [Fact]
    public void Http_CnMatch_ReturnsTrue()
    {
        var cert = ValidCert("host.example.com");

        cert.IsValid(RequestFor("https://host.example.com/api"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_CnMatch_IsCaseInsensitive()
    {
        var cert = ValidCert("Host.Example.Com");

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_NoMatchingCnOrSan_ReturnsFalse()
    {
        var cert = ValidCert("other.example.com");

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeFalse();
    }

    [Fact]
    public void Http_SanExactMatch_ReturnsTrue()
    {
        var cert = ValidCert("ignored-cn", san: ["host.example.com"]);

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_SanWildcard_SubdomainMatch_ReturnsTrue()
    {
        var cert = ValidCert("ignored-cn", san: ["*.example.com"]);

        cert.IsValid(RequestFor("https://sub.example.com"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_SanWildcard_RootDomainMatch_ReturnsTrue()
    {
        // *.example.com should also match example.com itself
        var cert = ValidCert("ignored-cn", san: ["*.example.com"]);

        cert.IsValid(RequestFor("https://example.com"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_SanWildcard_DifferentDomain_ReturnsFalse()
    {
        var cert = ValidCert("ignored-cn", san: ["*.example.com"]);

        cert.IsValid(RequestFor("https://sub.other.com"), cert, NullLogger.Instance)
            .ShouldBeFalse();
    }

    [Fact]
    public void Http_SanExactMatch_IsCaseInsensitive()
    {
        var cert = ValidCert("ignored-cn", san: ["Host.Example.Com"]);

        cert.IsValid(RequestFor("https://host.example.com"), cert, NullLogger.Instance)
            .ShouldBeTrue();
    }

    [Fact]
    public void Http_NullRequestUri_ReturnsFalse()
    {
        var cert = ValidCert("host.example.com");
        var message = new HttpRequestMessage(); // RequestUri is null

        cert.IsValid(message, cert, NullLogger.Instance).ShouldBeFalse();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

#pragma warning disable SYSLIB0026 // parameterless X509Certificate ctor is obsolete; no other way to produce a non-X509Certificate2 instance for the is-not check
    private sealed class BareX509Certificate : X509Certificate { }
#pragma warning restore SYSLIB0026
}
