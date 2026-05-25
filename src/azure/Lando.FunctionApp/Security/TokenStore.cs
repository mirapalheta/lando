using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;


namespace Lando.FunctionApp.Security;

/// <summary>
/// Azure Key Vault-backed <see cref="ITokenStore"/>. Refresh tokens live
/// as named secrets in Key Vault; access tokens live in <see cref="IMemoryCache"/>
/// scoped to the process lifetime and are minted from the refresh token via
/// <see cref="ITokenClient"/> on demand.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Secret naming.</strong> Key Vault secret names are limited to
/// <c>[A-Za-z0-9-]{1,127}</c>, while LWA <c>user_id</c> values look like
/// <c>amzn1.account.XXXXXXXX</c> (contains dots and is variable-length).
/// The store hashes the user_id to a stable, scheme-compliant suffix:
/// <c>{SecretPrefix}{lower(hex(SHA256(userId))[..32])}</c>. Hashing is for
/// formatting compliance only — these aren't security identifiers.
/// </para>
/// </remarks>
public sealed class TokenStore(string name, ITokenClient tokenClient, ISecretClient secretClient, IMemoryCache cache, ILogger<TokenStore> logger) : ITokenStore
{
    private static readonly Regex Sanitizer =
        new(@"[^a-zA-Z0-9-]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private const int AccessTokenSafetyBuffer = 60;

    private const string ListCacheKey = $"{nameof(TokenStore)}->List";

    /// <inheritdoc />
    public ITokenClient Client => tokenClient;

    /// <inheritdoc />
    public async Task SaveAsync(string id, string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        var secretName = SecretNameFor(id);
        logger.LogInformation("Persisting token id '{TokenId}' under secret '{SecretName}'", id, secretName);
        await secretClient.SetSecretAsync(secretName, refreshToken, cancellationToken).ConfigureAwait(false);

        // A new refresh token invalidates any cached access token derived from the prior one.
        cache.Remove(secretName);
        cache.Remove(ListCacheKey);
    }

    /// <inheritdoc />
    public async Task<(string value, int expiresIn)> GetAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));

        return await cache.GetOrCreateAsync($"{nameof(TokenStore)}:{id}", async entry =>
        {
            var token = await LoadTokenAsync(id, cancellationToken).ConfigureAwait(false);

            if (token.ExpiresIn > AccessTokenSafetyBuffer)
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(token.ExpiresIn - AccessTokenSafetyBuffer));

            return (token.AccessToken, token.ExpiresIn);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(string userId, string value)[]> ListAsync(CancellationToken cancellationToken)
        => await cache.GetOrCreateAsync(ListCacheKey, async entry =>
            {
                var tokens = await secretClient.ListKeysAsync(cancellationToken)
                    .Where(s => s.StartsWith($"{name}--", StringComparison.Ordinal))
                    .Select(async (s, ct) =>
                    {
                        var userId = s[(name.Length + 2)..];
                        var (token, expiresIn) = await GetAsync(userId, ct).ConfigureAwait(false);
                        return (userId, token, expiresIn);
                    })
                    .ToArrayAsync(cancellationToken);

                if (tokens.Length == 0)
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(AccessTokenSafetyBuffer));
                    return [];
                }

                var expiration = tokens.Min(t => t.expiresIn);
                if (expiration > AccessTokenSafetyBuffer)
                    entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(expiration - AccessTokenSafetyBuffer));
                return tokens.Select(t => (t.userId, t.token)).ToArray();
            }
        ).ConfigureAwait(false) ?? [];

    private async Task<Token> LoadTokenAsync(string id, CancellationToken cancellationToken)
    {
        var secretName = SecretNameFor(id);
        var token = await secretClient.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No refresh token found for '{id}'. (Key Vault secret '{secretName}' is missing.)");

        var minted = await tokenClient.RefreshAsync(token, cancellationToken).ConfigureAwait(false);

        // LWA may rotate the refresh token on refresh — persist the new one if returned.
        if (!string.IsNullOrEmpty(minted.RefreshToken) && minted.RefreshToken != token)
        {
            logger.LogInformation("rotated refresh token for grantee; persisting updated value");
            await secretClient.SetSecretAsync(secretName, minted.RefreshToken, cancellationToken).ConfigureAwait(false);
        }

        return minted;
    }

    // Key Vault requires names match [A-Za-z0-9-]{1,127}. Hash the user_id to a
    // stable, scheme-compliant suffix. Hash is for formatting, not security.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string SecretNameFor(string id)
        => $"{name}--{Sanitizer.Replace(id, "-")}";
}
