using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// State transformer for the <c>switch</c> HA domain. Reports only the
/// <c>Alexa.PowerController.powerState</c> property — switches have no further
/// state to surface.
/// </summary>
/// <remarks>
/// Mirrors <see cref="SwitchDiscoveryTransformer"/>: every retrievable capability
/// advertised at discovery time must have a corresponding reportable value here,
/// and switches advertise only PowerController.
/// </remarks>
public class SwitchStateTransformer : StateTransformerBase
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
    }
}
