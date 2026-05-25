namespace Lando.Security;

/// <summary>
/// Constructs per-purpose <see cref="ITokenStore"/> instances. The <c>name</c> parameter
/// is used by implementations to namespace persisted tokens (e.g., as a Key Vault secret
/// prefix), so multiple distinct token populations can coexist in the same backing store.
/// </summary>
public interface ITokenStoreFactory
{
    /// <summary>
    /// Creates a token store keyed by <paramref name="name"/>, using
    /// <paramref name="tokenClient"/> to refresh expired access tokens.
    /// </summary>
    ITokenStore Create(string name, ITokenClient tokenClient);
}
