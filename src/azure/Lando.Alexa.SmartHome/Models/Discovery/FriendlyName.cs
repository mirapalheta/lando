using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// One friendly name entry. Either an asset reference or a localized text label.
/// </summary>
public sealed class FriendlyName
{
    /// <summary>
    /// Discriminator — one of <see cref="FriendlyNameType"/>.
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = FriendlyNameType.Text;

    /// <summary>
    /// The value, shape determined by <see cref="Type"/>.
    /// </summary>
    [JsonPropertyName("value")]
    public FriendlyNameValue Value { get; set; } = new();
}
