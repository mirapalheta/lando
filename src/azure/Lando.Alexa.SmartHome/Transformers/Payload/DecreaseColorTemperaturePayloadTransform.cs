using System;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.ColorTemperatureController.DecreaseColorTemperature</c>.
/// Mirrors <see cref="IncreaseColorTemperaturePayloadTransform"/> but steps
/// the light toward a warmer colour, floored at <see cref="MinKelvin"/> —
/// roughly the perceived warmth of candlelight.
/// </summary>
/// <remarks>
/// Step size and default starting point are kept in lockstep with the
/// increase handler so successive "warmer"/"cooler" voice commands feel
/// symmetric.
/// </remarks>
public class DecreaseColorTemperaturePayloadTransform : IPayloadTransform<EmptyPayload>
{
    private const int StepKelvin = 500;
    private const int MinKelvin = 1900;
    private const int DefaultStartKelvin = 4000;

    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
    {
        var mireds = entity.Attributes.GetInt(EntityAttributes.ColorTemp);
        var current = mireds is int m and > 0 ? 1_000_000 / m : DefaultStartKelvin;
        var next = Math.Max(MinKelvin, current - StepKelvin);
        return HomeAssistantRequest.SetColorTemperature(entity.EntityId, next);
    }
}
