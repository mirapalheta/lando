using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// One endpoint returned to Alexa during discovery. An endpoint represents a single device
/// (a light, a thermostat, a scene) and its supported capabilities.
/// </summary>
public sealed class DiscoveryEndpoint
{
    /// <summary>
    /// Stable, opaque id the bridge assigns to the device. Echoed on every directive..
    /// </summary>
    [JsonPropertyName("endpointId")]
    public string EndpointId { get; set; } = null!;

    /// <summary>
    /// Device manufacturer or hub name (max 128 chars)..
    /// </summary>
    [JsonPropertyName("manufacturerName")]
    public string ManufacturerName { get; set; } = null!;

    /// <summary>
    /// Customer-visible description (max 128 chars)..
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Customer-visible name used by voice (max 128 chars). Avoid Alexa wake words..
    /// </summary>
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = null!;

    /// <summary>
    /// One or more <see cref="DisplayCategory"/> values..
    /// </summary>
    [JsonPropertyName("displayCategories")]
    public List<string> DisplayCategories { get; set; } = new();

    [JsonPropertyName("additionalAttributes")]
    public AdditionalAttributes? AdditionalAttributes { get; set; }

    [JsonPropertyName("capabilities")]
    public List<Capability> Capabilities { get; set; } = new();

    [JsonPropertyName("connections")]
    public List<EndpointConnection>? Connections { get; set; }

    [JsonPropertyName("relationships")]
    public Dictionary<string, EndpointRelationship>? Relationships { get; set; }

    /// <summary>
    /// Opaque per-endpoint state Alexa will echo back on every directive (cap 5 KB)..
    /// </summary>
    [JsonPropertyName("cookie")]
    public Dictionary<string, string>? Cookie { get; set; }

    public static DiscoveryEndpoint Create(string endpointId, string friendlyName, string category, IEnumerable<Capability> capabilities)
        => new()
        {
            EndpointId = endpointId,
            ManufacturerName = "Lando (Home Assistant)",
            Description = $"Home Assistant {category.ToFriendlyName()}",
            FriendlyName = friendlyName.Sanitize(),
            DisplayCategories = [category],
            Capabilities = [.. capabilities]
        };
}

/// <summary>
/// Hub/child relationship between endpoints. Currently used for <c>isConnectedBy</c>
/// to indicate which hub a device is reached through.
/// </summary>
public sealed class EndpointRelationship
{
    [JsonPropertyName("endpointId")]
    public string EndpointId { get; set; } = string.Empty;
}
