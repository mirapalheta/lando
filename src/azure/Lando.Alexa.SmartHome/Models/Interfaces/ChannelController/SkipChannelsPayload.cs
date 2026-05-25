using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ChannelController;

/// <summary>
/// Payload for <c>Alexa.ChannelController.SkipChannels</c>.
/// </summary>
public sealed class SkipChannelsPayload
{
    /// <summary>
    /// Number of channels to skip. Negative goes backwards.
    /// </summary>
    [JsonPropertyName("channelCount")]
    public int ChannelCount { get; set; }
}
