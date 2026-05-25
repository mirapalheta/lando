using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Friendly-name resources for a capability instance. Used by ModeController, RangeController
/// and ToggleController so the user can refer to the instance by name (e.g. "the fan speed").
/// </summary>
public sealed class CapabilityResources
{
    [JsonPropertyName("friendlyNames")]
    public List<FriendlyName> FriendlyNames { get; set; } = new();
}
