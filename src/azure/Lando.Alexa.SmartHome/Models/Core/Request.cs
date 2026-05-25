using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Top-level wire object Alexa POSTs to the Smart Home skill endpoint: <c>{ "directive": {...} }</c>.
/// </summary>
public sealed class Request
{
    /// <summary>
    /// The directive Alexa is asking the skill to handle..
    /// </summary>
    [JsonPropertyName("directive")]
    public Directive Directive { get; set; } = new();
}
