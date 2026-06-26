using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Discovery transformer for the <c>script</c> HA domain. Emits a single
/// <c>Alexa.SceneController</c> capability with the <c>ACTIVITY_TRIGGER</c>
/// display category. Scripts can be stopped via <c>script.turn_off</c>, so the
/// capability advertises <c>supportsDeactivation: true</c> and Alexa may send a
/// Deactivate directive that maps to stopping the running script.
/// </summary>
public class ScriptDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.ActivityTrigger;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
        => [Capability.SceneController(supportsDeactivation: true)];
}
