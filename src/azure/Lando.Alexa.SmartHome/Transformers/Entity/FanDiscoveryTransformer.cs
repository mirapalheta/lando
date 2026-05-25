using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>fan</c> HA domain. Always advertises
/// <c>Alexa.PowerController</c>; layers on the <see cref="Capability.FanSpeed"/>
/// RangeController (with low/medium/high presets and Raise/Lower semantics) when
/// the fan supports HA's <c>SET_SPEED</c> feature.
/// </summary>
/// <remarks>
/// The richer RangeController shape supersedes the older PercentageController for
/// fans — it gives natural utterances like "set the fan to medium" alongside
/// numeric speeds, and it renders consistently with how the shade transformer
/// advertises position.
/// </remarks>
public class FanDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.Fan;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        yield return Capability.PowerController;

        if ((entity.GetSupportedFeatures() & FanFeatures.SetSpeed) != 0)
            yield return Capability.FanSpeed;
    }
}
