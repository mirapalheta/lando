using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lando;

/// <summary>
/// Abstraction for a client that can retrieve secrets (e.g. from Key Vault).
/// </summary>
public interface ISecretClient
{
    /// <summary>
    /// Retrieves the secret value for the given key, or null if not found.
    /// </summary>
    /// <param name="key">The secret's key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the secret value for the given key, creating or overwriting as needed.
    /// </summary>
    /// <param name="key">The secret's key.</param>
    /// <param name="value">The secret's value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSecretAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the keys of all secrets, optionally filtered by a prefix.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of secret keys.</returns>
    IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken = default);
}
