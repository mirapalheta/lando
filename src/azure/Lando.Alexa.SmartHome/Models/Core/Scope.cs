using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Authorization scope passed on a directive endpoint or an outbound event endpoint.
/// The most common shape is <c>{"type":"BearerToken","token":"..."}</c>.
/// </summary>
/// <remarks>
/// Alexa also defines a <c>BearerTokenWithPartition</c> variant for room-aware skills.
/// </remarks>
public sealed class Scope
{
    /// <summary>
    /// Scope type, e.g. <c>"BearerToken"</c> or <c>"BearerTokenWithPartition"</c>..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = ScopeType.BearerToken;

    /// <summary>
    /// OAuth bearer token issued by LWA (Login with Amazon)..
    /// </summary>
    [JsonPropertyName("token")]
    public SecureString Token { get; set; }

    /// <summary>
    /// Partition (location/room) identifier. Only set when <see cref="Type"/> is partitioned..
    /// </summary>
    [JsonPropertyName("partition")]
    public string? Partition { get; set; }

    /// <summary>
    /// Partition-scoped user identifier. Only set when <see cref="Type"/> is partitioned..
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}

/// <summary>
/// Known <see cref="Scope.Type"/> values..
/// </summary>
public static class ScopeType
{
    public const string BearerToken = "BearerToken";
    public const string BearerTokenWithPartition = "BearerTokenWithPartition";
}
