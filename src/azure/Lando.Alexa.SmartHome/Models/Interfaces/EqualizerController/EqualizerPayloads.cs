using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.EqualizerController;

/// <summary>
/// One band of an equalizer setting, e.g. <c>{"name":"BASS","value":5}</c>..
/// </summary>
public sealed class EqualizerBand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

/// <summary>
/// Band with a relative adjust direction (<c>UP</c>/<c>DOWN</c>) and optional level delta..
/// </summary>
public sealed class EqualizerBandAdjustment
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("levelDelta")]
    public int LevelDelta { get; set; }

    /// <summary>
    /// One of <see cref="EqualizerLevelDirection"/>..
    /// </summary>
    [JsonPropertyName("levelDirection")]
    public string LevelDirection { get; set; } = EqualizerLevelDirection.Up;
}

/// <summary>
/// Payload for <c>Alexa.EqualizerController.SetBands</c>..
/// </summary>
public sealed class SetBandsPayload
{
    [JsonPropertyName("bands")]
    public List<EqualizerBand> Bands { get; set; } = new();
}

/// <summary>
/// Payload for <c>Alexa.EqualizerController.AdjustBands</c>..
/// </summary>
public sealed class AdjustBandsPayload
{
    [JsonPropertyName("bands")]
    public List<EqualizerBandAdjustment> Bands { get; set; } = new();
}

/// <summary>
/// Payload for <c>Alexa.EqualizerController.ResetBands</c>..
/// </summary>
public sealed class ResetBandsPayload
{
    [JsonPropertyName("bands")]
    public List<EqualizerBandName> Bands { get; set; } = new();
}

/// <summary>
/// Reference to a band by name only — used in ResetBands..
/// </summary>
public sealed class EqualizerBandName
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Payload for <c>Alexa.EqualizerController.SetMode</c>..
/// </summary>
public sealed class SetEqualizerModePayload
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}

/// <summary>
/// Known equalizer mode values..
/// </summary>
public static class EqualizerMode
{
    public const string Movie = "MOVIE";
    public const string Music = "MUSIC";
    public const string Night = "NIGHT";
    public const string Sport = "SPORT";
    public const string Tv = "TV";
}

/// <summary>
/// Known band level directions..
/// </summary>
public static class EqualizerLevelDirection
{
    public const string Up = "UP";
    public const string Down = "DOWN";
}

/// <summary>
/// Property names exposed by <c>Alexa.EqualizerController</c>..
/// </summary>
public static class EqualizerControllerProperties
{
    public const string Bands = "bands";
    public const string Mode = "mode";
}
