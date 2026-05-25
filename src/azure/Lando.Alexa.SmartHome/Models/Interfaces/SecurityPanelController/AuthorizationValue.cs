using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SecurityPanelController;

/// <summary>
/// The value object wrapping the PIN itself.
/// </summary>
public sealed class AuthorizationValue
{
    /// <summary>
    /// The PIN digits.
    /// </summary>
    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;
}
