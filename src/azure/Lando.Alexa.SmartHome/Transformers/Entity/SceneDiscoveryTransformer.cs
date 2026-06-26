using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Discovery transformer for the <c>scene</c> HA domain. Emits a single
/// <c>Alexa.SceneController</c> capability with the <c>SCENE_TRIGGER</c> display
/// category. HA scenes are fire-only (there is no <c>scene.turn_off</c>), so the
/// capability advertises <c>supportsDeactivation: false</c> and Alexa never
/// sends a Deactivate directive to these endpoints.
/// </summary>
public class SceneDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.SceneTrigger;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
        => [Capability.SceneController(supportsDeactivation: false)];
}
