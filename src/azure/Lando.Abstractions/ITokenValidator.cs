using System.Threading;
using System.Threading.Tasks;

namespace Lando;

/// <summary>
/// Validates an inbound bearer token before a request is dispatched.
/// </summary>
/// <remarks>
/// Concrete implementations decide what "valid" means — e.g. a Login-with-Amazon
/// profile lookup, a JWT signature check against a known JWKS, or a presence
/// probe in a database of active sessions. The shape returns a boolean rather
/// than throwing so callers can decide whether a soft <see langword="false"/>
/// should produce a 401 or be merged into a generic <c>InvalidAuthorizationCredential</c>
/// Alexa error response.
/// </remarks>
public interface ITokenValidator
{
    /// <summary>
    /// Validates the supplied token. Returns <see langword="true"/> when the
    /// token is well-formed, unexpired, and accepted by the upstream identity
    /// provider; <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="token">
    /// The raw bearer token string, or <see langword="null"/> when the caller
    /// failed to extract one from the request — in that case implementations
    /// should fast-path to <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsValidTokenAsync(string? token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload that unwraps a <see cref="SecureString"/> before
    /// delegating to the primary overload. Lets callers pass the redacted
    /// token type they already have without unwrapping at the call site.
    /// </summary>
    /// <param name="token">The wrapped bearer token, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsValidTokenAsync(SecureString token, CancellationToken cancellationToken = default)
        => IsValidTokenAsync(token.Value, cancellationToken);
}
