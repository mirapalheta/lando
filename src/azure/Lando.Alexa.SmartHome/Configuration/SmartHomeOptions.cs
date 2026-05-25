using Lando.Security;

namespace Lando.Alexa.SmartHome.Configuration;

/// <summary>
/// Configuration for the Alexa Smart Home skill identity that the bridge serves.
/// </summary>
/// <remarks>
/// <para>
/// Bound from the <see cref="SectionName"/> configuration section
/// (<c>Alexa:SmartHome</c> in app settings). Two credential sets are required:
/// <see cref="Authorization"/> and <see cref="Event"/>.
/// </para>
/// <para>
/// <see cref="Authorization"/> credentials are used for inbound bearer-token
/// validation: <c>Authorization.ClientId</c> is pinned against the <c>aud</c>
/// claim of every token Alexa attaches to a directive. Without it the bridge
/// can't tell which Smart Home skill minted the token.
/// </para>
/// <para>
/// <see cref="Event"/> credentials are used for outbound calls to the Alexa
/// Event Gateway: <c>Event.ClientId</c> / <c>Event.ClientSecret</c> are the
/// OAuth2 client used during <c>AcceptGrant</c> code exchange and for
/// subsequent refresh-token grants that mint short-lived access tokens for
/// proactive event delivery.
/// </para>
/// <para>
/// Find both credential pairs at
/// <c>https://developer.amazon.com/alexa/console/ask</c>: open your Smart
/// Home skill and go to the <c>Permissions</c> tab. Client IDs look like
/// <c>amzn1.application-oa2-client.XXXXXXXX</c>.
/// </para>
/// </remarks>
public class SmartHomeOptions
{
    /// <summary>
    /// Configuration section name bound to this options object.
    /// </summary>
    public const string SectionName = "Alexa:SmartHome";

    /// <summary>
    /// OAuth2 credentials whose <see cref="ClientCredentials.ClientId"/> is
    /// pinned against the <c>aud</c> claim of inbound directive bearer tokens.
    /// </summary>
    public ClientCredentials Authorization { get; init; } = new();

    /// <summary>
    /// OAuth2 credentials used for outbound calls to the Alexa Event Gateway —
    /// authorization-code exchange during <c>AcceptGrant</c> and subsequent
    /// refresh-token grants.
    /// </summary>
    public ClientCredentials Event { get; init; } = new();

}
