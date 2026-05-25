using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Additional, human-readable device metadata shown in the Alexa app and used for
/// debugging and analytics by Amazon.
/// </summary>
/// <remarks>
/// All fields are optional; populate the ones the upstream device exposes. Each value
/// is capped at 256 characters by Alexa.
/// </remarks>
public sealed class AdditionalAttributes
{
    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("firmwareVersion")]
    public string? FirmwareVersion { get; set; }

    [JsonPropertyName("softwareVersion")]
    public string? SoftwareVersion { get; set; }

    [JsonPropertyName("customIdentifier")]
    public string? CustomIdentifier { get; set; }
}
