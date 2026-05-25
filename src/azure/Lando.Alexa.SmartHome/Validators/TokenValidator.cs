using System;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Configuration;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Validators;

/// <summary>
/// Validates the bearer token Alexa attaches to control directives by calling
/// the Login-with-Amazon <c>tokeninfo</c> endpoint and checking the response's
/// <c>aud</c> claim matches the LWA client_id configured for this skill.
/// </summary>
/// <remarks>
/// <para>
/// The previous implementation called <c>https://api.amazon.com/user/profile</c>
/// and accepted any 200 OK — that proves the token belongs to *some* LWA user
/// session, but says nothing about which skill (or which user) the token was
/// minted for. Anyone with a valid LWA token from any LWA-enabled product
/// would have passed.
/// </para>
/// <para>
/// <c>tokeninfo</c> returns <c>{ iss, user_id, aud, app_id, exp }</c> where
/// <c>aud</c> is the LWA client_id of the application the token was issued for
/// and <c>exp</c> is the remaining lifetime in seconds. Pinning <c>aud</c> to
/// <c>SmartHomeOptions.Authorization.ClientId</c> shrinks the trust set to "tokens
/// minted by my Smart Home skill" — the smallest meaningful set for this
/// bridge.
/// </para>
/// <para>
/// Results are cached in <see cref="IMemoryCache"/> keyed by the token so the
/// hot path doesn't round-trip to Amazon on every directive. The cache TTL is
/// the smaller of the token's remaining lifetime (minus a 60s safety buffer)
/// and a hard cap of one hour.
/// </para>
/// </remarks>
public class TokenValidator([FromKeyedServices(Constants.TokenStore)] ITokenClient client, IMemoryCache cache, IOptions<SmartHomeOptions> options, ILogger<TokenValidator> logger) : ITokenValidator
{
    private static readonly TimeSpan MaxCacheTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheSafetyBuffer = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    public async Task<bool> IsValidTokenAsync(string? token, CancellationToken cancellationToken)
        => !string.IsNullOrWhiteSpace(token)
        && await cache.GetOrCreateAsync($"{nameof(TokenValidator)}:{token}", async entry =>
        {
            try
            {
                var info = await client.GetAsync(token, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(info?.Aud))
                {
                    logger.LogWarning("Token introspection returned an empty or malformed payload");
                    return false;
                }

                if (!string.Equals(info.Aud, options.Value.Authorization.ClientId, StringComparison.Ordinal))
                {
                    logger.LogWarning("Token aud claim {Aud} does not match the configured Alexa skill client_id; rejecting request", info.Aud);
                    return false;
                }

                if (info.Exp <= 0)
                {
                    logger.LogWarning("Token introspection returned non-positive exp ({Exp}); treating as expired", info.Exp);
                    return false;
                }

                // exp is seconds remaining, not an absolute timestamp.
                var lifetime = TimeSpan.FromSeconds(info.Exp) - CacheSafetyBuffer;
                if (lifetime > MaxCacheTtl)
                    lifetime = MaxCacheTtl;
                if (lifetime > TimeSpan.Zero)
                    entry.SetAbsoluteExpiration(lifetime);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error validating token");
                return false;
            }
        }).ConfigureAwait(false);
}
