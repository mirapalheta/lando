using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// State transformer shared by the <c>scene</c> and <c>script</c> domains.
/// <c>Alexa.SceneController</c> exposes no retrievable controller state, so this
/// reports no domain properties — the base class still stamps
/// <c>Alexa.EndpointHealth</c>, the only property these endpoints advertise.
/// </summary>
/// <remarks>
/// Registered under both the <c>scene</c> and <c>script</c> domain keys; the two
/// discovery transformers differ but the (empty) state projection is identical.
/// </remarks>
public class SceneControllerStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity) => [];
}
