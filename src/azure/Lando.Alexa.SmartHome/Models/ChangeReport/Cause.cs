using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ChangeReport;

/// <summary>
/// Cause attribution attached to each change in a <c>ChangeReport</c>.
/// </summary>
public sealed class Cause
{
    /// <summary>
    /// One of <see cref="ChangeCauseType"/>'s constants..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = ChangeCauseType.PhysicalInteraction;
}
