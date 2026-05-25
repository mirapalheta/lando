using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PlaybackController;

/// <summary>
/// The set of supported operations declared on a discovered
/// <c>Alexa.PlaybackController</c> capability.
/// </summary>
public sealed class PlaybackOperationsConfiguration
{
    /// <summary>
    /// The operations the device supports — see <see cref="PlaybackOperations"/>.
    /// </summary>
    [JsonPropertyName("supportedOperations")]
    public List<string> SupportedOperations { get; set; } = new();
}
