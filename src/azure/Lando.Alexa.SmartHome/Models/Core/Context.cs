using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Context block on a synchronous or asynchronous response — the full current state of the
/// endpoint as the skill knows it.
/// </summary>
/// <remarks>
/// Alexa's guidance is to return <em>all</em> retrievable properties for the endpoint here,
/// not only the ones the directive changed.
/// </remarks>
public sealed class Context
{
    [JsonPropertyName("properties")]
    public List<ContextProperty> Properties { get; set; } = new();
}
