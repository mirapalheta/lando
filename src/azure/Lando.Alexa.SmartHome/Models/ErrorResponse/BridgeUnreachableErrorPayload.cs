using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// Variant emitted when the hub/bridge that fronts the device is offline.
/// </summary>
public sealed class BridgeUnreachableErrorPayload : ErrorPayload
{
    public BridgeUnreachableErrorPayload() : base(ErrorType.BridgeUnreachable) { }

    [JsonPropertyName("resolutionType")]
    public string? ResolutionType { get; set; }

    [JsonPropertyName("resolutionMessage")]
    public string? ResolutionMessage { get; set; }
}
