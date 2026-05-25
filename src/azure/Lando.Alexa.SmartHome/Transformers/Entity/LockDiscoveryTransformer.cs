using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Discovery transformer for the <c>lock</c> HA domain. Advertises
/// <c>Alexa.LockController</c> with the <c>SMARTLOCK</c> display category.
/// </summary>
public class LockDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.SmartLock;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
        => [Capability.LockController];
}
