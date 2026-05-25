using System;
using System.Collections.Generic;
using System.Linq;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>light</c> HA domain. Always advertises
/// <c>Alexa.PowerController</c>; layers on <c>Alexa.BrightnessController</c>,
/// <c>Alexa.ColorTemperatureController</c>, and <c>Alexa.ColorController</c> based
/// on the light's <c>supported_color_modes</c> attribute.
/// </summary>
/// <remarks>
/// Branches on the modern <c>supported_color_modes</c> attribute rather than the
/// legacy <c>SUPPORT_*</c> bits on <c>supported_features</c>, since HA core has
/// deprecated those for lights. Any colour-aware mode implies brightness, so the
/// brightness controller is always added alongside a colour-aware light.
/// </remarks>
public class LightDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.Light;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        yield return Capability.PowerController;

        var modes = entity.GetSupportedColorModes();

        // Anything other than pure on/off implies a dimmable bulb.
        if (modes.Except([LightColorModes.OnOff], StringComparer.OrdinalIgnoreCase).Any())
            yield return Capability.BrightnessController;

        if (modes.Contains(LightColorModes.ColorTemp, StringComparer.OrdinalIgnoreCase))
            yield return Capability.ColorTemperatureController;

        if (modes.Intersect(LightColorModes.ChromaticModes, StringComparer.OrdinalIgnoreCase).Any())
            yield return Capability.ColorController;
    }
}
