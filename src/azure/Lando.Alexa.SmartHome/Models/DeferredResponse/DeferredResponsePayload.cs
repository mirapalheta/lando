using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.DeferredResponse;

/// <summary>
/// Payload for <c>Alexa.DeferredResponse</c>. Returned synchronously when the bridge needs
/// longer than Alexa's 8-second timeout to complete a directive (locks, WoL, snapshot
/// providers, automotive endpoints). The bridge follows with the real
/// <c>Alexa.Response</c> over the asynchronous event gateway.
/// </summary>
public sealed class DeferredResponsePayload
{
    /// <summary>
    /// Approximate seconds the customer should wait before hearing the result..
    /// </summary>
    [JsonPropertyName("estimatedDeferralInSeconds")]
    public int? EstimatedDeferralInSeconds { get; set; }
}
