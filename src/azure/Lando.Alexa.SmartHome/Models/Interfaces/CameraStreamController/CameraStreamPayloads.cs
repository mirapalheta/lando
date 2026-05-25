using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.CameraStreamController;

/// <summary>
/// Resolution descriptor used both when Alexa asks for a stream
/// (<c>InitiateRequest</c> payloads) and when the bridge declares its supported
/// resolutions on a discovered capability.
/// </summary>
public sealed class CameraStreamResolution
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

/// <summary>
/// One requested or returned stream descriptor..
/// </summary>
public sealed class CameraStream
{
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = CameraStreamProtocol.Hls;

    [JsonPropertyName("resolution")]
    public CameraStreamResolution Resolution { get; set; } = new();

    [JsonPropertyName("authorizationType")]
    public string AuthorizationType { get; set; } = CameraStreamAuthorizationType.None;

    [JsonPropertyName("videoCodec")]
    public string VideoCodec { get; set; } = CameraVideoCodec.H264;

    [JsonPropertyName("audioCodec")]
    public string AudioCodec { get; set; } = CameraAudioCodec.Aac;

    // ---------- Response-only fields ----------

    /// <summary>
    /// URI the Alexa device should connect to. Only set on responses..
    /// </summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary>
    /// Expiration time (ISO-8601 UTC) for the stream URI. Only set on responses..
    /// </summary>
    [JsonPropertyName("expirationTime")]
    public string? ExpirationTime { get; set; }

    /// <summary>
    /// How many seconds Alexa may keep the stream idle before closing..
    /// </summary>
    [JsonPropertyName("idleTimeoutSeconds")]
    public int? IdleTimeoutSeconds { get; set; }
}

/// <summary>
/// Inbound payload for <c>Alexa.CameraStreamController.InitializeCameraStreams</c>..
/// </summary>
public sealed class InitializeCameraStreamsPayload
{
    [JsonPropertyName("cameraStreams")]
    public List<CameraStream> CameraStreams { get; set; } = new();
}

/// <summary>
/// Outbound payload for <c>Alexa.CameraStreamController.Response</c>..
/// </summary>
public sealed class CameraStreamsResponsePayload
{
    [JsonPropertyName("cameraStreams")]
    public List<CameraStream> CameraStreams { get; set; } = new();

    /// <summary>
    /// Optional JPEG snapshot URI (single image preview)..
    /// </summary>
    [JsonPropertyName("imageUri")]
    public string? ImageUri { get; set; }
}

/// <summary>
/// Discovery-time configuration for the CameraStreamController..
/// </summary>
public sealed class CameraStreamConfiguration
{
    [JsonPropertyName("cameraStreamConfigurations")]
    public List<CameraStreamConfigurationEntry> CameraStreamConfigurations { get; set; } = new();
}

/// <summary>
/// One entry of supported protocol/resolution/codec combinations..
/// </summary>
public sealed class CameraStreamConfigurationEntry
{
    [JsonPropertyName("protocols")]
    public List<string> Protocols { get; set; } = new();

    [JsonPropertyName("resolutions")]
    public List<CameraStreamResolution> Resolutions { get; set; } = new();

    [JsonPropertyName("authorizationTypes")]
    public List<string> AuthorizationTypes { get; set; } = new();

    [JsonPropertyName("videoCodecs")]
    public List<string> VideoCodecs { get; set; } = new();

    [JsonPropertyName("audioCodecs")]
    public List<string> AudioCodecs { get; set; } = new();
}

/// <summary>
/// Known stream transport protocols..
/// </summary>
public static class CameraStreamProtocol
{
    public const string Hls = "HLS";
    public const string Rtsp = "RTSP";
    public const string WebRtc = "WEBRTC";
}

/// <summary>
/// Known authorization types for the stream URI..
/// </summary>
public static class CameraStreamAuthorizationType
{
    public const string None = "NONE";
    public const string Basic = "BASIC";
    public const string Digest = "DIGEST";
}

/// <summary>
/// Known video codecs..
/// </summary>
public static class CameraVideoCodec
{
    public const string H264 = "H264";
    public const string Mpeg2 = "MPEG2";
    public const string Mjpeg = "MJPEG";
    public const string Jpg = "JPG";
}

/// <summary>
/// Known audio codecs..
/// </summary>
public static class CameraAudioCodec
{
    public const string Aac = "AAC";
    public const string G711 = "G711";
    public const string None = "NONE";
}
