using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Maps a state utterance (e.g. <c>Alexa.States.Open</c>) onto a property value.
/// </summary>
public sealed class StateMapping
{
    /// <summary>
    /// Mapping discriminator — always <c>"StatesToValue"</c> or <c>"StatesToRange"</c>.
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "StatesToValue";

    /// <summary>
    /// State ids that map to <see cref="Value"/> or <see cref="Range"/>.
    /// </summary>
    [JsonPropertyName("states")]
    public List<string> States { get; set; } = new();

    /// <summary>
    /// Discrete property value for <c>StatesToValue</c> mappings.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Range descriptor for <c>StatesToRange</c> mappings.
    /// </summary>
    [JsonPropertyName("range")]
    public SupportedRange? Range { get; set; }
}
