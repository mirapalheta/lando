using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Per-interface configuration block on a discovered capability. Different interfaces define
/// different shapes — RangeController uses <see cref="SupportedRange"/> and <see cref="Presets"/>,
/// ModeController uses <see cref="SupportedModes"/>, and so on.
/// </summary>
/// <remarks>
/// All members are nullable so each interface fills in only what's relevant to it.
/// </remarks>
public sealed class CapabilityConfiguration
{
    // ---------- ModeController ----------
    [JsonPropertyName("ordered")]
    public bool? Ordered { get; set; }

    [JsonPropertyName("supportedModes")]
    public List<object>? SupportedModes { get; set; }

    // ---------- RangeController ----------
    [JsonPropertyName("supportedRange")]
    public SupportedRange? SupportedRange { get; set; }

    [JsonPropertyName("unitOfMeasure")]
    public string? UnitOfMeasure { get; set; }

    [JsonPropertyName("presets")]
    public List<Preset>? Presets { get; set; }

    // ---------- ThermostatController ----------
    // Thermostat reports its supported modes via the standard `supportedModes` list of
    // strings on the capability — handled by callers that build the capability JSON directly,
    // because the wire shape there differs from ModeController.
    // Uses the shared SupportedModes property above (object? serialised as string[]).

    [JsonPropertyName("supportsScheduling")]
    public bool? SupportsScheduling { get; set; }

    // ---------- EqualizerController ----------
    [JsonPropertyName("bands")]
    public EqualizerBandsConfig? Bands { get; set; }

    [JsonPropertyName("modes")]
    public EqualizerModesConfig? Modes { get; set; }
}
