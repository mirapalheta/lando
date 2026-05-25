using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>fan</c> HA domain. Reports PowerController for the
/// on/off state and, when present, the <see cref="Capability.FanSpeedInstance"/>
/// RangeController rangeValue for the current speed.
/// </summary>
/// <remarks>
/// Mirrors <see cref="FanDiscoveryTransformer"/>: the legacy PercentageController
/// is no longer advertised at discovery, so this transformer must not report a
/// PercentageController value either — doing so would surface a property Alexa
/// would silently drop.
/// </remarks>
public class FanStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        yield return new ContextProperty
        {
            Namespace = Namespaces.PowerController,
            Name = PowerControllerProperties.PowerState,
            Value = entity.State == "on" ? PowerState.On : PowerState.Off,
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };

        if (entity.Attributes.GetInt(EntityAttributes.Percentage) is int percentage)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.RangeController,
                Instance = Capability.FanSpeedInstance,
                Name = RangeControllerProperties.RangeValue,
                Value = percentage,
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }
    }
}
