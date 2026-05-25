using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Marker for an explicitly empty JSON object payload (<c>{}</c>). Many Smart Home responses
/// (TurnOn, SetBrightness, etc.) carry no body but the wire requires the property be present.
/// </summary>
public sealed class EmptyPayload
{
    [JsonConstructor]
    private EmptyPayload() { }

    /// <summary>
    /// Shared singleton — the type has no state..
    /// </summary>
    public static readonly EmptyPayload Instance = new();
}
