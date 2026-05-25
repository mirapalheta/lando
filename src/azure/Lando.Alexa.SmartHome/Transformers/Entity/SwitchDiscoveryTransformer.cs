using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;
/// <summary>
/// Discovery transformer for the <c>switch</c> HA domain. Emits a single
/// <c>Alexa.PowerController</c> capability with the <c>SWITCH</c> display category —
/// switches are pure binary devices and need no further configuration.
/// </summary>
/// <remarks>
/// Kept as a discrete transformer (rather than handled inline in a multi-domain
/// branch) so the discovery dispatcher stays uniform and the test surface mirrors
/// the production registration shape.
/// </remarks>
public class SwitchDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.Switch;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        yield return Capability.PowerController;
    }
}
