using System;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.ColorTemperatureController.IncreaseColorTemperature</c>.
/// Alexa sends only the directive name — no delta — so the bridge picks the
/// step size, caps it at <see cref="MaxKelvin"/>, and dispatches the new
/// absolute kelvin value to HA's <c>light.turn_on</c> service.
/// </summary>
/// <remarks>
/// 500K is the empirical sweet spot — small enough to feel like one step
/// per voice command, large enough that the colour shift is visible against
/// the ambient lighting.
/// </remarks>
public class IncreaseColorTemperaturePayloadTransform : IPayloadTransform<EmptyPayload>
{
    private const int StepKelvin = 500;
    private const int MaxKelvin = 7000;
    private const int DefaultStartKelvin = 4000;

    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
    {
        // HA stores color temperature as mireds; kelvin = 1_000_000 / mireds.
        var mireds = entity.Attributes.GetInt(EntityAttributes.ColorTemp);
        var current = mireds is int m and > 0 ? 1_000_000 / m : DefaultStartKelvin;
        var next = Math.Min(MaxKelvin, current + StepKelvin);
        return HomeAssistantRequest.SetColorTemperature(entity.EntityId, next);
    }
}
