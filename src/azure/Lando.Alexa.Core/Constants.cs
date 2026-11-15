using System.Net.Http;
using Lando.Alexa.Security.HMAC;

namespace Lando.Alexa;

/// <summary>
/// String constants shared across the Alexa core layer: HMAC configuration
/// keys + header names, and the canonical names used to register the bridge's
/// named <see cref="HttpClient"/> instances. Centralising these
/// here keeps DI registration code and the verifier in lockstep — a renamed
/// header here surfaces as a compile break, not a runtime 401.
/// </summary>
public static class Constants
{
    /// <summary>
    /// HMAC-related configuration and wire-format constants.
    /// </summary>
    public static class Hmac
    {
        /// <summary>
        /// Configuration section name bound to <see cref="HmacOptions"/>.
        /// </summary>
        public const string SectionName = "Hmac";

        /// <summary>
        /// HTTP header names emitted by the Lambda signer and read by the verifier.
        /// </summary>
        public static class Headers
        {
            /// <summary>
            /// Header carrying the Unix-seconds timestamp covered by the signature.
            /// </summary>
            public const string TimestampHeader = "X-Lando-Timestamp";

            /// <summary>
            /// Header carrying the <c>&lt;version&gt;=&lt;hex&gt;</c> signature value.
            /// </summary>
            public const string SignatureHeader = "X-Lando-Signature";
        }
    }

    /// <summary>
    /// DI keys for the named <see cref="HttpClient"/>
    /// instances registered by <c>AddAlexa</c>. Pass these to
    /// <see cref="IHttpClientFactory.CreateClient(string)"/>
    /// (or the <c>GetHttpClient</c> extension) at the consumer side.
    /// </summary>
    public static class HttpClients
    {
        /// <summary>
        /// Named client for the Amazon authorization API (<c>api.amazon.com/auth/o2</c>).
        /// </summary>
        public static class AmazonApi
        {
            /// <summary>
            /// The DI key used to register the client in the service collection, and to resolve it at the consumer side. Should be a stable constant, never a type name.
            /// </summary>
            public const string Name = nameof(AmazonApi);

            /// <summary>
            /// The base URL for the Amazon authorization API, used to exchange Login with Amazon tokens for user profile info.
            /// This is not an endpoint the bridge calls directly, but it's used in tests to verify the correct client is injected into the token-exchange service.
            /// </summary>
            public const string Url = "https://api.amazon.com/auth/o2/";

            /// <summary>
            /// The timeout for calls to the Amazon API. This is a hard timeout — the API should respond quickly, and if it doesn't respond at all we don't want to wait indefinitely.
            /// </summary>
            public const int TimeoutSeconds = 10;
        }

        /// <summary>
        /// Named client for the Alexa Event Gateway (<c>api.amazonalexa.com</c>).
        /// </summary>
        public static class AlexaApi
        {
            /// <summary>
            /// The DI key used to register the client in the service collection, and to resolve it at the consumer side. Should be a stable constant, never a type name.
            /// </summary>
            public const string Name = nameof(AlexaApi);

            /// <summary>
            /// The base URL for the Alexa Event Gateway, used for proactive directives and other calls from the bridge to Alexa.
            /// This is not an endpoint the bridge receives requests on, but it's used in tests to verify the correct client is injected into proactive directive handlers.
            /// </summary>
            public const string Url = "https://api.amazonalexa.com/";

            /// <summary>
            /// The timeout for calls to the Amazon API. This is a hard timeout — the API should respond quickly, and if it doesn't respond at all we don't want to wait indefinitely.
            /// <see cref="HttpClient.Timeout"/> applies to each request, not to a retry sequence, so it is only the ceiling above <see cref="AttemptTimeoutSeconds"/>.
            /// </summary>
            public const int TimeoutSeconds = 10;

            /// <summary>
            /// How many times one event is sent to the Event Gateway before it is given up as lost. Amazon asks for a failed ChangeReport to be resent at least twice.
            /// </summary>
            public const int MaxAttempts = 3;

            /// <summary>
            /// The timeout for one attempt. The gateway normally answers in about 0.1 s and Alexa expects a ChangeReport within 3 s of the change. Must stay below <see cref="TimeoutSeconds"/>, or the client's own timeout fires first.
            /// </summary>
            public const int AttemptTimeoutSeconds = 3;

            /// <summary>
            /// The pause between two attempts. Amazon asks for resends at least 1 s apart.
            /// </summary>
            public const int RetryDelaySeconds = 1;
        }
    }
}
