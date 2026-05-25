using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Semantic mapping that lets users speak natural utterances ("open the blinds") and have
/// Alexa translate them into directives on a generic capability ("SetRangeValue: 100").
/// Optional and currently supported on RangeController, ModeController, and ToggleController.
/// </summary>
public sealed class CapabilitySemantics
{
    [JsonPropertyName("actionMappings")]
    public List<ActionMapping> ActionMappings { get; set; } = new();

    [JsonPropertyName("stateMappings")]
    public List<StateMapping> StateMappings { get; set; } = new();
}
