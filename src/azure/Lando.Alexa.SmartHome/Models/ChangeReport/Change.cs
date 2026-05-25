using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Models.ChangeReport;

/// <summary>
/// The body of a <c>ChangeReport</c> payload. Lists the properties that changed and why,
/// while the surrounding <see cref="Response.Context"/> reports the full current state.
/// </summary>
public sealed class Change
{
    [JsonPropertyName("cause")]
    public Cause Cause { get; set; } = new();

    [JsonPropertyName("properties")]
    public List<ContextProperty> Properties { get; set; } = new();
}
