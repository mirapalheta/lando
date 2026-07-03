using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// A resolved intent: its name plus any filled slots.
/// </summary>
public sealed class Intent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slots")]
    public Dictionary<string, Slot>? Slots { get; set; }
}
