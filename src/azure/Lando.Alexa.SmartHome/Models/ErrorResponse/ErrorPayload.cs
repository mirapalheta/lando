using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// Base payload for an <c>Alexa.ErrorResponse</c>. Interface-specific errors extend this
/// with extra members (e.g. <see cref="ValueOutOfRangeErrorPayload.ValidRange"/>).
/// </summary>
public class ErrorPayload(string errorType, string message)
{
    public ErrorPayload(ErrorType errorType = ErrorType.InternalError, string message = "") : this(errorType.ToErrorCode(), message) { }

    /// <summary>
    /// One of <see cref="ErrorType"/>'s constants..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = errorType;

    /// <summary>
    /// Human-readable description, surfaced in Alexa skill logs for debugging..
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = message;
}
