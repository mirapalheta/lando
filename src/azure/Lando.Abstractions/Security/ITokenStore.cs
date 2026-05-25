using System.Threading;
using System.Threading.Tasks;

namespace Lando.Security;

/// <summary>
/// Per-grantee persistence + retrieval seam for tokens.
/// </summary>
/// <remarks>
/// <para>
/// At <c>Alexa.Authorization.AcceptGrant</c> time the bridge exchanges the
/// inbound authorization code for a refresh/access token pair and persists
/// the refresh token here, keyed by the grantee's LWA user_id. Later, when
/// posting a ChangeReport or deferred Response to the Event Gateway, the
/// bridge asks this store for a *fresh* access token — the implementation is
/// responsible for caching, refreshing, and (where applicable) re-persisting
/// rotated refresh tokens.
/// </para>
/// <para>
/// Keeping this an abstraction (the concrete implementation lives outside
/// <c>Lando.Alexa.SmartHome</c>, in the Azure-aware FunctionApp project) keeps
/// the Smart Home layer free of any cloud-vendor coupling.
/// </para>
/// </remarks>
public interface ITokenStore
{
    /// <summary>
    /// The client used to exchange refresh tokens for access tokens, and to introspect tokens.
    /// </summary>
    ITokenClient Client { get; }

    /// <summary>
    /// Persists the refresh token for the given grantee. Overwrites any prior
    /// value — repeated AcceptGrant flows for the same grantee are normal when
    /// the customer disables and re-enables the skill.
    /// </summary>
    /// <param name="id">The <c>user_id</c> of the grantee.</param>
    /// <param name="refreshToken">The LWA refresh token to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(string id, string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a non-expired access token for the given grantee. Implementations
    /// should cache access tokens in-memory and only refresh against LWA when
    /// the cache misses or the token is within a safety window of expiry.
    /// </summary>
    /// <param name="id">The <c>user_id</c> of the grantee.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored access token and its remaining lifetime in seconds.</returns>
    Task<(string value, int expiresIn)> GetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the <c>user_id</c> values and their corresponding tokens of all saved tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<(string userId, string value)[]> ListAsync(CancellationToken cancellationToken);
}
