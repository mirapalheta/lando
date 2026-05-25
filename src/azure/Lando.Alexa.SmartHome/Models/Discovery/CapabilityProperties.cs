using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Describes the set of properties a capability exposes plus whether Alexa may read them
/// (<see cref="Retrievable"/>) and whether the skill will proactively report changes
/// (<see cref="ProactivelyReported"/>).
/// </summary>
public sealed class CapabilityProperties
{
    [JsonPropertyName("supported")]
    public List<CapabilityPropertyName> Supported { get; set; } = new();

    [JsonPropertyName("proactivelyReported")]
    public bool ProactivelyReported { get; set; } = true;

    [JsonPropertyName("retrievable")]
    public bool Retrievable { get; set; } = true;

    /// <summary>
    /// True when this property can be tracked for state history..
    /// </summary>
    [JsonPropertyName("nonControllable")]
    public bool? NonControllable { get; set; }
}

/// <summary>A single supported property name — wraps the string so the JSON shape is
/// <c>{ "name": "powerState" }</c> as Alexa expects.</summary>
public sealed class CapabilityPropertyName
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    public CapabilityPropertyName() { }
    public CapabilityPropertyName(string name) { Name = name; }

    public static implicit operator CapabilityPropertyName(string name) => new(name);
}
