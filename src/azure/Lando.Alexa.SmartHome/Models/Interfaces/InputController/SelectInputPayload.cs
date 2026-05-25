using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.InputController;

/// <summary>
/// Payload for <c>Alexa.InputController.SelectInput</c>..
/// </summary>
public sealed class SelectInputPayload
{
    /// <summary>
    /// Input identifier — e.g. <c>HDMI 1</c>, <c>HDMI 2</c>, <c>TUNER</c>, <c>AUX 1</c>, <c>BLURAY</c>..
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;
}
