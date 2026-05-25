using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// The directive that should be invoked when the matched action is heard.
/// </summary>
public sealed class SemanticDirective
{
    /// <summary>
    /// Directive name (e.g. <c>"SetRangeValue"</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directive payload, shape determined by the directive name.
    /// </summary>
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new();
}
