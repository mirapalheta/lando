using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome;

/// <summary>
/// Defines a service that transforms a control directive payload into a Home Assistant request.
/// </summary>
/// <typeparam name="TPayload">The type of the directive payload.</typeparam>
public interface IPayloadTransform<TPayload>
    where TPayload : class
{
    /// <summary>
    /// Transforms the directive payload into a Home Assistant request.
    /// The entity ID is provided separately since directive payloads do not contain it.
    /// </summary>
    /// <param name="entity">The Home Assistant entity to target.</param>
    /// <param name="payload">The directive payload to transform.</param>
    /// <returns>A Home Assistant request representing the control directive request.</returns>
    HomeAssistantRequest Transform(HomeAssistantEntity entity, TPayload payload);
}
