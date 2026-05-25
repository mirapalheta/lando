using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// The value object for a <see cref="FriendlyName"/>.
/// </summary>
public sealed class FriendlyNameValue
{
    /// <summary>
    /// Used when the parent is <see cref="FriendlyNameType.Text"/>.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// BCP-47 locale, required when <see cref="Text"/> is set.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Used when the parent is <see cref="FriendlyNameType.Asset"/>; references an
    /// <c>Alexa.Asset.Value.*</c> asset id.
    /// </summary>
    [JsonPropertyName("assetId")]
    public string? AssetId { get; set; }
}
