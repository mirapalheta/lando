using System;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// One reported property on the <see cref="Context"/> of an outbound response. Reports the
/// current value of a property on the endpoint along with how recently the value was sampled.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Value"/> is intentionally a plain <see cref="object"/> because property values
/// span every JSON shape across interfaces: a string enum for <c>powerState</c>, an integer
/// percentage for <c>brightness</c>, a <c>Temperature</c> object, an HSB object, etc.
/// </para>
/// </remarks>
public sealed class ContextProperty
{
    /// <summary>
    /// The interface namespace this property belongs to (e.g. <c>"Alexa.PowerController"</c>)..
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Optional instance id for multi-instance interfaces..
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// The property name within the interface (e.g. <c>"powerState"</c>)..
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The current property value. Shape depends on the interface — see the per-interface
    /// payload classes for strongly typed representations.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// UTC timestamp the value was sampled at..
    /// </summary>
    [JsonPropertyName("timeOfSample")]
    public DateTime TimeOfSample { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How stale the value can be at most, in milliseconds..
    /// </summary>
    [JsonPropertyName("uncertaintyInMilliseconds")]
    public int UncertaintyInMilliseconds { get; set; }
}
