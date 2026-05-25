using System.Text.Json.Serialization;

namespace Lando.HomeAssistant.Models;

/// <summary>
/// JSON payload posted to the Home Assistant <c>services/&lt;domain&gt;/&lt;service&gt;</c>
/// endpoint. Constructed exclusively through the static factory methods on
/// this class so each service call is paired with the exact set of fields HA
/// expects for that service — callers cannot accidentally send a
/// <c>temperature</c> attribute to a <c>light.turn_on</c> call, for example.
/// </summary>
/// <remarks>
/// <para>
/// Every property except <see cref="EntityId"/> is nullable; the JSON
/// serialiser is configured to omit nulls, so each factory only sets the
/// fields meaningful for its target service. <see cref="Service"/> is
/// <see cref="JsonIgnoreAttribute"/>'d because it determines the URL, not the
/// body, of the HA REST call.
/// </para>
/// </remarks>
public sealed class HomeAssistantRequest
{
    private HomeAssistantRequest(string entityId, string service)
    {
        Service = service;
        EntityId = entityId;
    }

    /// <summary>
    /// HA service name (e.g. <c>"turn_on"</c>) used by the caller to build the
    /// <c>services/&lt;domain&gt;/&lt;service&gt;</c> URL. Not serialised.
    /// </summary>
    [JsonIgnore]
    public string Service { get; }

    /// <summary>
    /// HA entity id to target (e.g. <c>"light.kitchen"</c>).
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string EntityId { get; }

    /// <summary>
    /// Relative brightness adjustment in percent, used by <see cref="AdjustBrightness"/>.
    /// </summary>
    [JsonPropertyName("brightness_step_pct")]
    public int? BrightnessStepPercent { get; private set; }

    /// <summary>
    /// HS color as <c>[hue, saturation%]</c>, used by <see cref="SetLightColor"/>.
    /// </summary>
    [JsonPropertyName("hs_color")]
    public double[]? HsColor { get; private set; }

    /// <summary>
    /// Absolute brightness in percent (0–100).
    /// </summary>
    [JsonPropertyName("brightness_pct")]
    public double? Brightness { get; private set; }

    /// <summary>
    /// Color temperature in Kelvin.
    /// </summary>
    [JsonPropertyName("kelvin")]
    public int? Kelvin { get; private set; }

    /// <summary>
    /// HVAC mode value (e.g. <c>"heat"</c>, <c>"cool"</c>).
    /// </summary>
    [JsonPropertyName("hvac_mode")]
    public string? HvacMode { get; private set; }

    /// <summary>
    /// Single setpoint temperature.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; private set; }

    /// <summary>
    /// Lower setpoint when using a heat/cool dual-setpoint mode.
    /// </summary>
    [JsonPropertyName("target_temp_low")]
    public double? TargetTempLow { get; private set; }

    /// <summary>
    /// Upper setpoint when using a heat/cool dual-setpoint mode.
    /// </summary>
    [JsonPropertyName("target_temp_high")]
    public double? TargetTempHigh { get; private set; }

    /// <summary>
    /// Mute state for <c>media_player.volume_mute</c>.
    /// </summary>
    [JsonPropertyName("is_volume_muted")]
    public bool? IsVolumeMuted { get; private set; }

    /// <summary>
    /// Volume level on a 0.0–1.0 scale.
    /// </summary>
    [JsonPropertyName("volume_level")]
    public double? VolumeLevel { get; private set; }

    /// <summary>
    /// Percentage value used by fan / generic percentage services.
    /// </summary>
    [JsonPropertyName("percentage")]
    public int? Percentage { get; private set; }

    /// <summary>
    /// Cover position in percent (0–100).
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; private set; }

    /// <summary>
    /// Builds a relative-brightness adjustment via <c>light.turn_on</c>.
    /// </summary>
    public static HomeAssistantRequest AdjustBrightness(string entityId, int brightness)
        => new(entityId, Constants.Services.TurnOn)
        {
            BrightnessStepPercent = brightness
        };

    /// <summary>
    /// Builds a color-temperature change via <c>light.turn_on</c>.
    /// </summary>
    public static HomeAssistantRequest SetColorTemperature(string entityId, int colorTemperature)
        => new(entityId, Constants.Services.TurnOn)
        {
            Kelvin = colorTemperature
        };

    /// <summary>
    /// Builds an HS-color light change via <c>light.turn_on</c>. Hue is in
    /// degrees, saturation/brightness in percent (0–100).
    /// </summary>
    public static HomeAssistantRequest SetLightColor(string entityId, double hue, double saturationPercent, double brightnessPercent)
        => new(entityId, Constants.Services.TurnOn)
        {
            HsColor = [hue, saturationPercent],
            Brightness = brightnessPercent
        };

    /// <summary>
    /// Builds a <c>cover.close_cover</c> request.
    /// </summary>
    public static HomeAssistantRequest CloseCover(string entityId)
        => new(entityId, Constants.Services.CloseCover);

    /// <summary>
    /// Builds a <c>cover.open_cover</c> request.
    /// </summary>
    public static HomeAssistantRequest OpenCover(string entityId)
        => new(entityId, Constants.Services.OpenCover);

    /// <summary>
    /// Builds a <c>cover.set_cover_position</c> request with an absolute position (0–100).
    /// </summary>
    public static HomeAssistantRequest SetCoverPosition(string entityId, int position)
        => new(entityId, Constants.Services.SetCoverPosition)
        {
            Position = position
        };

    /// <summary>
    /// Builds a <c>lock.lock</c> request.
    /// </summary>
    public static HomeAssistantRequest Lock(string entityId)
        => new(entityId, Constants.Services.Lock);

    /// <summary>
    /// Builds a <c>lock.unlock</c> request.
    /// </summary>
    public static HomeAssistantRequest Unlock(string entityId)
        => new(entityId, Constants.Services.Unlock);

    /// <summary>
    /// Builds a <c>climate.set_hvac_mode</c> request.
    /// </summary>
    public static HomeAssistantRequest SetHvacMode(string entityId, string hvacMode)
        => new(entityId, Constants.Services.SetHvacMode)
        {
            HvacMode = hvacMode
        };

    /// <summary>
    /// Builds a single-setpoint <c>climate.set_temperature</c> request.
    /// </summary>
    public static HomeAssistantRequest SetTemperature(string entityId, double temperature)
        => new(entityId, Constants.Services.SetTemperature)
        {
            Temperature = temperature
        };

    /// <summary>
    /// Builds a dual-setpoint <c>climate.set_temperature</c> request.
    /// </summary>
    public static HomeAssistantRequest SetTemperature(string entityId, double low, double high)
        => new(entityId, Constants.Services.SetTemperature)
        {
            TargetTempLow = low,
            TargetTempHigh = high
        };

    /// <summary>
    /// Builds a <c>media_player.volume_mute</c> request.
    /// </summary>
    public static HomeAssistantRequest SetMute(string entityId, bool isMuted)
        => new(entityId, Constants.Services.SetMute)
        {
            IsVolumeMuted = isMuted
        };

    /// <summary>
    /// Builds a <c>media_player.volume_set</c> request with a 0.0–1.0 volume level.
    /// </summary>
    public static HomeAssistantRequest SetVolume(string entityId, double volume)
        => new(entityId, Constants.Services.SetVolume)
        {
            VolumeLevel = volume
        };

    /// <summary>
    /// Builds a <c>fan.set_percentage</c> request.
    /// </summary>
    public static HomeAssistantRequest SetPercentage(string entityId, int percentage)
        => new(entityId, Constants.Services.SetPercentage)
        {
            Percentage = percentage
        };

    /// <summary>
    /// Builds a <c>turn_on</c> request, optionally including an initial
    /// brightness when targeting a light entity.
    /// </summary>
    public static HomeAssistantRequest TurnOn(string entityId, int? brightness = null)
        => new(entityId, Constants.Services.TurnOn)
        {
            Brightness = brightness
        };

    /// <summary>
    /// Builds a <c>turn_off</c> request.
    /// </summary>
    public static HomeAssistantRequest TurnOff(string entityId)
        => new(entityId, Constants.Services.TurnOff);
}
