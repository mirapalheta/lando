using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ChannelController;

/// <summary>
/// Channel metadata (display name and logo) that Alexa may include alongside the channel identifier.
/// </summary>
public sealed class ChannelMetadata
{
    /// <summary>
    /// Display name for the channel.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// URI of a channel logo image.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
