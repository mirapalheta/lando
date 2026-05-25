using System;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lando.FunctionApp.Security;

/// <summary>
/// Azure-aware <see cref="ITokenStoreFactory"/>. Constructs Key Vault-backed
/// <see cref="TokenStore"/> instances keyed by the supplied name, wiring each
/// one with the shared <see cref="KeyVaultSecretClient"/>, the per-process
/// <see cref="IMemoryCache"/>, and a typed logger.
/// </summary>
public sealed class TokenStoreFactory(ISecretClient secretClient, IMemoryCache cache, ILoggerFactory loggerFactory) : ITokenStoreFactory
{
    /// <summary>
    /// Builds a <see cref="TokenStore"/> bound to <paramref name="name"/> and
    /// <paramref name="tokenClient"/>. The name is used to namespace persisted
    /// secrets in Key Vault, so distinct token populations (e.g. one per
    /// skill) coexist without colliding.
    /// </summary>
    /// <param name="name">The namespace prefix for Key Vault secret names.</param>
    /// <param name="tokenClient">The token client used to refresh expired access tokens.</param>
    /// <returns>A configured <see cref="TokenStore"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenClient"/> is null.</exception>
    public ITokenStore Create(string name, ITokenClient tokenClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(tokenClient, nameof(tokenClient));

        var logger = loggerFactory.CreateLogger<TokenStore>();
        logger.LogInformation("Creating token store with name {Name}", name);
        return new TokenStore(name, tokenClient, secretClient, cache, logger);
    }
}
