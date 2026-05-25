using System.Threading;
using System.Threading.Tasks;

namespace Lando.Security;

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
public interface ITokenClient
{
    /// <summary>
    /// Resolves a bearer token to its introspection payload. Returns
    /// <see langword="null"/> if the token is invalid, expired, or the call
    /// fails for any reason.
    /// </summary>
    Task<TokenInfo?> GetAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges an authorization code (from AcceptGrant) for an access/refresh token pair.
    /// </summary>
    Task<Token> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Trades a stored refresh token for a fresh access token. The response may
    /// or may not include a new refresh token; callers should overwrite the
    /// stored refresh token when one is returned.
    /// </summary>
    Task<Token> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
