using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Maps an action utterance (e.g. <c>Alexa.Actions.Open</c>) onto a directive.
/// </summary>
public sealed class ActionMapping
{
    /// <summary>
    /// Mapping discriminator — always <c>"ActionsToDirective"</c>.
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "ActionsToDirective";

    /// <summary>
    /// Action ids that trigger this mapping.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// The directive Alexa should send when a matching action is heard.
    /// </summary>
    [JsonPropertyName("directive")]
    public SemanticDirective Directive { get; set; } = new();
}
