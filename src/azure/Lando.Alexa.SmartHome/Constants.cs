using Lando.Alexa.SmartHome.Handlers;
using Lando.Security;

namespace Lando.Alexa.SmartHome;

/// <summary>
/// String constants shared across the Alexa Smart Home layer: the DI key for
/// the per-skill <see cref="ITokenStore"/>, the custom-attribute keys the
/// bridge reads off Home Assistant entities, the Azure Functions trigger
/// metadata, and the separator characters used to translate between HA
/// entity ids (<c>switch.kitchen</c>) and Alexa endpoint ids
/// (<c>switch#kitchen</c>).
/// </summary>
public static class Constants
{
    /// <summary>
    /// DI key under which the per-skill <see cref="ITokenStore"/> is
    /// registered. The same key resolves the LWA refresh-token store used by
    /// <c>AcceptGrantDirectiveHandler</c> and the <see cref="Services.EventGatewayClient"/>.
    /// </summary>
    public const string TokenStore = "alexa-smarthome-token";

    /// <summary>
    /// Alexa-specific overrides users can set on HA entity <c>attributes</c>
    /// to influence how the bridge surfaces the entity to Alexa. Layered on
    /// top of the bridge-wide <c>lando_*</c> attributes — a present
    /// <c>alexa_*</c> wins for the Alexa integration but doesn't affect any
    /// other consumer of the bridge.
    /// </summary>
    public static class CustomAttributes
    {
        /// <summary>
        /// Override of the Alexa display category for this entity.
        /// </summary>
        public const string Display = "alexa_display";

        /// <summary>
        /// Per-skill exposure flag — overrides the bridge-wide <c>lando_expose</c>.
        /// </summary>
        public const string Expose = "alexa_expose";

        /// <summary>
        /// Per-skill friendly name — overrides the bridge-wide <c>lando_name</c>.
        /// </summary>
        public const string Name = "alexa_name";
    }

    /// <summary>
    /// Azure Functions trigger metadata for the Smart Home endpoint. Centralised
    /// so the <c>[Function]</c> attribute, the route, and the DI key for the
    /// matching <c>SmartHomeHandler</c> all stay in lockstep.
    /// </summary>
    public static class Function
    {
        /// <summary>
        /// Function name used by the <c>[Function]</c> attribute.
        /// </summary>
        public const string Name = "Alexa-SmartHome";

        /// <summary>
        /// DI key under which the <see cref="SmartHomeHandler"/> + its validator are registered.
        /// </summary>
        public const string Handler = nameof(SmartHomeHandler);

        /// <summary>
        /// Relative HTTP route the Smart Home endpoint listens on.
        /// </summary>
        public const string Route = "alexa/smart-home";
    }

    /// <summary>
    /// Separator characters used when translating between Alexa endpoint ids and
    /// Home Assistant entity ids. Discovery and ChangeReport keep the two
    /// formats in lockstep — sending a ChangeReport keyed on the dotted HA
    /// form silently fails to route to a known endpoint.
    /// </summary>
    public static class Separators
    {
        /// <summary>
        /// Separator in Alexa endpoint ids (e.g. <c>switch#kitchen</c>).
        /// </summary>
        public const char Alexa = '#';

        /// <summary>
        /// Separator in HA entity ids (e.g. <c>switch.kitchen</c>).
        /// </summary>
        public const char HomeAssistant = '.';
    }
}
