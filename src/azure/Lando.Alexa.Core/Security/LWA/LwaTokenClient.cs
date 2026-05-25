using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Security;
using Microsoft.Extensions.Logging;

namespace Lando.Alexa.Security.LWA;

/// <summary>
/// Talks to the Login-with-Amazon token endpoint
/// (<c>https://api.amazon.com/auth/o2/token</c>) to exchange authorization
/// codes for refresh/access token pairs, and to refresh access tokens against
/// stored refresh tokens.
/// </summary>
/// <remarks>
/// <para>
/// Used by the <c>AcceptGrantDirectiveHandler</c> (in the SmartHome layer) at
/// skill-enable time, and by <see cref="ITokenStore"/> implementations whenever
/// a cached access token has expired and needs to be re-minted from the
/// stored refresh token.
/// </para>
/// <para>
/// Endpoint contract per
/// <c>https://developer.amazon.com/docs/login-with-amazon/access-token.html</c>:
/// the request is <c>application/x-www-form-urlencoded</c>; the response is
/// JSON shaped <c>{ access_token, refresh_token, token_type, expires_in }</c>
/// where <c>expires_in</c> is seconds until the access token expires.
/// </para>
/// </remarks>
public sealed class LwaTokenClient(HttpClient client, ClientCredentials credentials, ILogger<LwaTokenClient> logger) : ITokenClient
{
    private const string TokenEndpoint = "token";
    private const string TokenInfoEndpoint = "tokeninfo";

    /// <inheritdoc />
    public async Task<TokenInfo?> GetAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{TokenInfoEndpoint}?access_token={Uri.EscapeDataString(token)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            return await response
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<TokenInfo>(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Introspection failures are non-fatal — the caller treats null as "token unusable"
            // and falls back to refresh. Log at warning so the failure is visible without
            // implying an outage.
            logger.LogWarning(ex, "Error introspecting LWA token");
            return null;
        }
    }

    /// <inheritdoc />
    public Task<Token> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        => PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
        }, cancellationToken);

    /// <inheritdoc />
    public Task<Token> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        => PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    private async Task<Token> PostAsync(IDictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = credentials.ClientId;
        form["client_secret"] = credentials.ClientSecret;
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            // LWA returns a JSON error envelope (RFC 6749 §5.2). Read it raw so the message
            // makes it to the log even on parse failure; never log secrets.
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("LWA token endpoint returned {StatusCode}: {Body}", response.StatusCode, error);
            throw new LwaTokenException($"LWA token endpoint returned {(int)response.StatusCode}");
        }

        var token = await response.Content
            .ReadFromJsonAsync<Token>(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(token?.AccessToken))
        {
            logger.LogWarning("LWA token endpoint returned empty or malformed payload");
            throw new LwaTokenException("LWA token response missing access_token");
        }

        return token;
    }
}
