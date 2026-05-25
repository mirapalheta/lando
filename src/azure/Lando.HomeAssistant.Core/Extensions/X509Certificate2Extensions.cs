using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace System.Security.Cryptography.X509Certificates;

/// <summary>
/// Server-certificate validation helpers used by the keyed HA HTTP / WebSocket
/// handlers when the user has configured a custom CA. The pinning model is
/// thumbprint + validity window + hostname (CN or SAN match) — the bridge
/// intentionally does not rely on the system trust store when a custom cert is
/// supplied, so a Tailscale-internal HA instance signed by a private CA can be
/// trusted without weakening validation for anything else.
/// </summary>
public static class X509Certificate2Extensions
{
    extension(X509Certificate? certificate)
    {
        /// <summary>
        /// WebSocket-handshake variant — there is no <see cref="HttpRequestMessage"/>
        /// at that point, so hostname validation is deferred. Delegates to the
        /// <see cref="HttpRequestMessage"/>-aware overload with a null message.
        /// </summary>
        /// <param name="customCaCert">The pinned CA certificate to validate against.</param>
        /// <param name="logger">Logger for emitting reasons that validation failed.</param>
        /// <returns><c>true</c> when the server certificate matches the pin and is in-validity.</returns>
        public bool IsValid(X509Certificate2 customCaCert, ILogger logger)
        {
            if (certificate is not X509Certificate2 cert2)
            {
                logger.LogWarning("WebSocket TLS: no server certificate presented");
                return false;
            }

            return cert2.IsValid(default, customCaCert, logger);
        }
    }

    extension(X509Certificate2? certificate)
    {
        /// <summary>
        /// HTTP variant. Validates the server certificate against
        /// <paramref name="customCaCert"/> by thumbprint, then enforces validity
        /// window, then (when <paramref name="message"/> is supplied) hostname
        /// match against the request URI.
        /// </summary>
        /// <param name="message">
        /// The outgoing request, used to read the expected hostname for SAN/CN
        /// matching. <c>null</c> at WebSocket handshake time — the call ignores
        /// hostname matching in that case.
        /// </param>
        /// <param name="customCaCert">The pinned CA certificate to validate against.</param>
        /// <param name="logger">Logger for emitting reasons that validation failed.</param>
        /// <returns><c>true</c> when all checks pass.</returns>
        public bool IsValid(HttpRequestMessage? message, X509Certificate2 customCaCert, ILogger logger)
        {
            if (certificate == null)
            {
                logger.LogWarning("No server certificate provided");
                return false;
            }

            // 1. Validate certificate thumbprint (pinning)
            if (!certificate.Thumbprint.Equals(customCaCert.Thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Certificate thumbprint validation failed");
                return false;
            }

            // 2. Validate certificate is not expired
            if (DateTime.UtcNow < certificate.NotBefore || DateTime.UtcNow > certificate.NotAfter)
            {
                logger.LogWarning("Certificate is expired or not yet valid");
                return false;
            }

            // 3. Validate hostname matches certificate CN/SAN only when message is available (i.e. during HTTP requests, not WebSocket handshake)
            if (message == null)
                return true;

            var expectedHostname = message.RequestUri?.Host;
            if (string.IsNullOrEmpty(expectedHostname))
            {
                logger.LogWarning("Request URI is null or has no host");
                return false;
            }

            if (!certificate.IsValid(expectedHostname))
            {
                logger.LogWarning("Hostname validation failed: {Host}", expectedHostname);
                return false;
            }

            return true;
        }

        private bool IsValid(string hostname)
        {
            // Check CN (Common Name)
            var cn = certificate!.GetNameInfo(X509NameType.SimpleName, false);
            if (cn?.Equals(hostname, StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Check SAN (Subject Alternative Names)
            if (certificate.Extensions["2.5.29.17"] is not X509SubjectAlternativeNameExtension san)
                return false;

            foreach (var name in san.EnumerateDnsNames())
            {
                if (name.Equals(hostname, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!name.StartsWith("*."))
                    continue;

                var domain = name[2..];
                if (hostname.Equals(domain, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (hostname.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
