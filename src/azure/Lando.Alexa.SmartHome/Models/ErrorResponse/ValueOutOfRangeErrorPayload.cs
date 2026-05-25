using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// Variant emitted for <see cref="ErrorType.ValueOutOfRange"/> and
/// <see cref="ErrorType.TemperatureValueOutOfRange"/> errors. <see cref="ValidRange"/>
/// communicates the acceptable bounds for the requested property.
/// </summary>
public sealed class ValueOutOfRangeErrorPayload : ErrorPayload
{
    public ValueOutOfRangeErrorPayload() : base(ErrorType.ValueOutOfRange) { }

    [JsonPropertyName("validRange")]
    public ValidRange? ValidRange { get; set; }
}

/// <summary>
/// The valid range, expressed as <c>minimumValue</c>/<c>maximumValue</c>..
/// </summary>
public sealed class ValidRange
{
    /// <summary>
    /// Minimum acceptable value. Type varies — int for percentages, Temperature for thermostats..
    /// </summary>
    [JsonPropertyName("minimumValue")]
    public object? MinimumValue { get; set; }

    [JsonPropertyName("maximumValue")]
    public object? MaximumValue { get; set; }
}
