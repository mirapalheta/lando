using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ChannelController;

/// <summary>
/// Payload for <c>Alexa.ChannelController.ChangeChannel</c>.
/// </summary>
public sealed class ChangeChannelPayload
{
    /// <summary>
    /// The channel identifier.
    /// </summary>
    [JsonPropertyName("channel")]
    public Channel Channel { get; set; } = new();

    /// <summary>
    /// Optional metadata Alexa supplies alongside the channel identifier.
    /// </summary>
    [JsonPropertyName("channelMetadata")]
    public ChannelMetadata? ChannelMetadata { get; set; }
}
