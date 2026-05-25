using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SecurityPanelController;

/// <summary>
/// PIN authorization block.
/// </summary>
public sealed class Authorization
{
    /// <summary>
    /// Authorization scheme — currently always <c>FOUR_DIGIT_PIN</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "FOUR_DIGIT_PIN";

    /// <summary>
    /// The wrapped PIN value.
    /// </summary>
    [JsonPropertyName("value")]
    public AuthorizationValue Value { get; set; } = new();
}
