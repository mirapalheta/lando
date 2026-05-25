namespace Lando.HomeAssistant.Models;

using static Lando.Alexa.SmartHome.Constants;

internal static class HomeAssistantStateChangedEventExtensions
{
    /// <summary>
    /// Turns a Home Assistant entity id from a service-call event into the form Alexa wants on outbound discovery and state events.
    /// </summary>
    /// <param name="event">The Home Assistant state change event.</param>
    /// <returns>
    /// The corresponding Alexa endpoint id, with <c>.</c> swapped to <c>#</c>.
    /// </returns>
    public static string EndpointId(this HomeAssistantStateChangedEvent @event)
        => @event.EntityId.Replace(Separators.HomeAssistant, Separators.Alexa);
}
