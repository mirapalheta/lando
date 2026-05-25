using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// One mode declared on a ModeController capability.
/// </summary>
public sealed class SupportedMode
{
    /// <summary>
    /// The mode identifier (e.g. <c>"Color.Red"</c>).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Friendly-name resources describing the mode to the user.
    /// </summary>
    [JsonPropertyName("modeResources")]
    public CapabilityResources? ModeResources { get; set; }
}
