using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ChannelController;

/// <summary>
/// Identifies a TV channel. At least one of (<see cref="Number"/>, <see cref="CallSign"/>,
/// <see cref="AffiliateCallSign"/>, <see cref="Uri"/>) is supplied; the others are null.
/// </summary>
public sealed class Channel
{
    /// <summary>
    /// Channel number as a string (e.g. <c>"5.1"</c>).
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Network call sign (e.g. <c>"PBS"</c>).
    /// </summary>
    [JsonPropertyName("callSign")]
    public string? CallSign { get; set; }

    /// <summary>
    /// Local affiliate call sign.
    /// </summary>
    [JsonPropertyName("affiliateCallSign")]
    public string? AffiliateCallSign { get; set; }

    /// <summary>
    /// Stream URI for IP-delivered channels.
    /// </summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}
