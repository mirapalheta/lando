using System;

namespace Lando.HomeAssistant.Configuration;

/// <summary>
/// URI helpers that derive the HA WebSocket and REST endpoints from a single
/// <see cref="HomeAssistantClientOptions.BaseUrl"/>, so the rest of the code
/// can be written against one configuration value.
/// </summary>
public static class HomeAssistantClientOptionsExtensions
{
    extension(HomeAssistantClientOptions options)
    {
        /// <summary>
        /// Builds a WebSocket URI based on the <see cref="HomeAssistantClientOptions.BaseUrl"/>.
        /// The scheme is switched from <c>http</c>/<c>https</c> to <c>ws</c>/<c>wss</c> and the
        /// path is set to <c>/api/websocket</c>.
        /// </summary>
        /// <returns>The WebSocket endpoint URI.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="HomeAssistantClientOptions.BaseUrl"/> uses a scheme other
        /// than <c>http</c> or <c>https</c>.
        /// </exception>
        public Uri WebSocketUri()
        {
            var uri = new UriBuilder(options.BaseUrl) { Path = "/api/websocket" };
            uri.Scheme = uri.Scheme switch
            {
                "https" => "wss",
                "http" => "ws",
                var s => throw new InvalidOperationException($"Unsupported URI scheme in HA BaseUrl: {s}")
            };
            return uri.Uri;
        }

        /// <summary>
        /// Builds the HA REST API base URI by rooting the configured
        /// <see cref="HomeAssistantClientOptions.BaseUrl"/> at <c>/api/</c>.
        /// </summary>
        /// <returns>The REST API base URI used by the typed HTTP client.</returns>
        public Uri ApiUri()
            => new UriBuilder(options.BaseUrl) { Path = "/api/" }.Uri;
    }
}
