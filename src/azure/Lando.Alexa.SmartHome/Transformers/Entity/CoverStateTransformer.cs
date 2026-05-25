using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>cover</c> HA domain. Reports a single property —
/// either the <c>Shade.Position</c> rangeValue for positionable shade-likes or the
/// <c>powerState</c> for binary covers — keeping the state report in lockstep with
/// the capability set chosen by <see cref="CoverDiscoveryTransformer"/>.
/// </summary>
/// <remarks>
/// Reporting both <c>powerState</c> and <c>rangeValue</c> on a shade would push
/// the Alexa app back to the light-style horizontal slider with a power pill —
/// the very thing the discovery shape was designed to avoid — so the two paths
/// stay mutually exclusive.
/// </remarks>
public class CoverStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        var hasSetPosition = (entity.GetSupportedFeatures() & CoverFeatures.SetPosition) != 0;
        var isShade = CoverDiscoveryTransformer.IsShadeLike(entity.GetDeviceClass());

        if (hasSetPosition && isShade)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.RangeController,
                Instance = Capability.ShadePositionInstance,
                Name = RangeControllerProperties.RangeValue,
                Value = entity.Attributes.GetInt(EntityAttributes.CurrentPosition) ?? 0,
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
            yield break;
        }

        yield return new ContextProperty
        {
            Namespace = Namespaces.PowerController,
            Name = PowerControllerProperties.PowerState,
            Value = entity.State switch
            {
                "open" or "opening" or "on" => PowerState.On,
                _ => PowerState.Off
            },
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };
    }
}
