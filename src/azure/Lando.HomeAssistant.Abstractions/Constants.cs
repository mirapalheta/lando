namespace Lando.HomeAssistant;

/// <summary>
/// String / numeric constants the bridge uses to talk to Home Assistant.
/// Centralising these here keeps the discovery service, capability builders,
/// and payload transformers in lockstep — a renamed attribute key surfaces
/// here as a compile break, not as a silent runtime mismatch.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Canonical short name for the Home Assistant integration.
    /// </summary>
    public const string HomeAssistant = nameof(HomeAssistant);

    /// <summary>
    /// Custom attribute keys Lando reads from the HA entity <c>attributes</c>
    /// blob. Users add these in their HA <c>customize.yaml</c> (or per-entity
    /// configuration) to opt entities into the bridge and control their
    /// display.
    /// </summary>
    public static class CustomAttributes
    {
        /// <summary>
        /// Truthy value opts an entity into Lando discovery.
        /// </summary>
        public const string Expose = "lando_expose";

        /// <summary>
        /// Optional override of the Alexa display category Lando would otherwise
        /// pick automatically (e.g. force a light into <c>SWITCH</c>).
        /// </summary>
        public const string Display = "lando_display";

        /// <summary>
        /// Optional override of the friendly name advertised to Alexa, used when
        /// the HA <c>friendly_name</c> isn't how the customer wants to address
        /// the device by voice.
        /// </summary>
        public const string Name = "lando_name";
    }

    internal static class Services
    {
        public const string HomeAssistant = "homeassistant";
        public const string Script = "script";
        public const string Scene = "scene";
        public const string Unlock = "unlock";
        public const string Lock = "lock";
        public const string TurnOn = "turn_on";
        public const string TurnOff = "turn_off";
        public const string SetVolume = "volume_set";
        public const string SetTemperature = "set_temperature";
        public const string SetPercentage = "set_percentage";
        public const string SetMute = "volume_mute";
        public const string SetHvacMode = "set_hvac_mode";
        public const string SetCoverPosition = "set_cover_position";
        public const string OpenCover = "open_cover";
        public const string CloseCover = "close_cover";
    }

    /// <summary>
    /// HA entity-id domain prefixes (e.g. <c>light</c>, <c>switch</c>) the
    /// bridge recognises. The discovery filter and per-domain transformers
    /// both key off these strings.
    /// </summary>
    public static class Domains
    {
        /// <summary>
        /// Sentinel used when an entity id lacks a recognisable prefix.
        /// </summary>
        public const string Unknown = "unknown";
        /// <summary>
        /// HA <c>alarm_control_panel</c> domain.
        /// </summary>
        public const string AlarmControlPanel = "alarm_control_panel";
        /// <summary>
        /// HA <c>automation</c> domain.
        /// </summary>
        public const string Automation = "automation";
        /// <summary>
        /// HA <c>binary_sensor</c> domain.
        /// </summary>
        public const string BinarySensor = "binary_sensor";
        /// <summary>
        /// HA <c>button</c> domain.
        /// </summary>
        public const string Button = "button";
        /// <summary>
        /// HA <c>calendar</c> domain.
        /// </summary>
        public const string Calendar = "calendar";
        /// <summary>
        /// HA <c>camera</c> domain.
        /// </summary>
        public const string Camera = "camera";
        /// <summary>
        /// HA <c>climate</c> domain (thermostats, AC, heat pumps).
        /// </summary>
        public const string Climate = "climate";
        /// <summary>
        /// HA <c>cover</c> domain (blinds, garage doors, shades).
        /// </summary>
        public const string Cover = "cover";
        /// <summary>
        /// HA <c>device_tracker</c> domain.
        /// </summary>
        public const string DeviceTracker = "device_tracker";
        /// <summary>
        /// HA <c>fan</c> domain.
        /// </summary>
        public const string Fan = "fan";
        /// <summary>
        /// HA <c>humidifier</c> domain.
        /// </summary>
        public const string Humidifier = "humidifier";
        /// <summary>
        /// HA <c>input_boolean</c> domain — exposes user-defined toggles.
        /// </summary>
        public const string InputBoolean = "input_boolean";
        /// <summary>
        /// HA <c>light</c> domain.
        /// </summary>
        public const string Light = "light";
        /// <summary>
        /// HA <c>lock</c> domain.
        /// </summary>
        public const string Lock = "lock";
        /// <summary>
        /// HA <c>media_player</c> domain.
        /// </summary>
        public const string MediaPlayer = "media_player";
        /// <summary>
        /// HA <c>person</c> domain.
        /// </summary>
        public const string Person = "person";
        /// <summary>
        /// HA <c>remote</c> domain.
        /// </summary>
        public const string Remote = "remote";
        /// <summary>
        /// HA <c>scene</c> domain.
        /// </summary>
        public const string Scene = "scene";
        /// <summary>
        /// HA <c>script</c> domain.
        /// </summary>
        public const string Script = "script";
        /// <summary>
        /// HA <c>sensor</c> domain.
        /// </summary>
        public const string Sensor = "sensor";
        /// <summary>
        /// HA <c>switch</c> domain.
        /// </summary>
        public const string Switch = "switch";
        /// <summary>
        /// HA <c>vacuum</c> domain.
        /// </summary>
        public const string Vacuum = "vacuum";
        /// <summary>
        /// HA <c>water_heater</c> domain.
        /// </summary>
        public const string WaterHeater = "water_heater";
        /// <summary>
        /// HA <c>weather</c> domain.
        /// </summary>
        public const string Weather = "weather";
    }

    /// <summary>
    /// Names of attributes Home Assistant places on the entity's <c>attributes</c> blob.
    /// Centralized here so the discovery service and capability builders agree on keys.
    /// </summary>
    public static class EntityAttributes
    {
        /// <summary>
        /// HA-set human-readable label for the entity.
        /// </summary>
        public const string FriendlyName = "friendly_name";
        /// <summary>
        /// HA device class (e.g. <c>door</c>, <c>temperature</c>, <c>tv</c>).
        /// </summary>
        public const string DeviceClass = "device_class";
        /// <summary>
        /// Legacy per-domain feature bitmask (covers, fans, climate, media players).
        /// </summary>
        public const string SupportedFeatures = "supported_features";
        /// <summary>
        /// List of HA color-mode strings a light supports.
        /// </summary>
        public const string SupportedColorModes = "supported_color_modes";
        /// <summary>
        /// Unit-of-measurement string accompanying a sensor reading.
        /// </summary>
        public const string UnitOfMeasurement = "unit_of_measurement";
        /// <summary>
        /// Current brightness on a 0–255 scale.
        /// </summary>
        public const string Brightness = "brightness";
        /// <summary>
        /// Current color temperature in mireds.
        /// </summary>
        public const string ColorTemp = "color_temp";
        /// <summary>
        /// List of supported HVAC mode strings.
        /// </summary>
        public const string HvacModes = "hvac_modes";
        /// <summary>
        /// Current ambient temperature reported by a climate device.
        /// </summary>
        public const string CurrentTemperature = "current_temperature";
        /// <summary>
        /// Setpoint temperature on a climate device.
        /// </summary>
        public const string Temperature = "temperature";
        /// <summary>
        /// Minimum allowed setpoint for a climate device.
        /// </summary>
        public const string MinTemp = "min_temp";
        /// <summary>
        /// Maximum allowed setpoint for a climate device.
        /// </summary>
        public const string MaxTemp = "max_temp";
        /// <summary>
        /// Current ambient humidity reading.
        /// </summary>
        public const string CurrentHumidity = "current_humidity";
        /// <summary>
        /// Lower setpoint when a climate device uses a dual-setpoint mode.
        /// </summary>
        public const string TargetTempLow = "target_temp_low";
        /// <summary>
        /// Upper setpoint when a climate device uses a dual-setpoint mode.
        /// </summary>
        public const string TargetTempHigh = "target_temp_high";
        /// <summary>
        /// Percentage value reported by fan / percentage entities.
        /// </summary>
        public const string Percentage = "percentage";
        /// <summary>
        /// Current cover position in percent (0–100).
        /// </summary>
        public const string CurrentPosition = "current_position";

        // ---------- media_player ----------

        /// <summary>
        /// Current media-player volume level on a 0.0–1.0 scale.
        /// </summary>
        public const string VolumeLevel = "volume_level";
        /// <summary>
        /// Whether the media player is currently muted.
        /// </summary>
        public const string IsVolumeMuted = "is_volume_muted";
        /// <summary>
        /// Currently selected input source on a media player.
        /// </summary>
        public const string Source = "source";
        /// <summary>
        /// List of selectable input sources for a media player.
        /// </summary>
        public const string SourceList = "source_list";
    }

    /// <summary>
    /// Modern HA color-mode strings — the source of truth for what a light can do.
    /// Replaces the deprecated <c>SUPPORT_*</c> bitmask values for color/brightness.
    /// </summary>
    public static class LightColorModes
    {
        /// <summary>
        /// Light supports only on/off — no brightness or color.
        /// </summary>
        public const string OnOff = "onoff";
        /// <summary>
        /// Light supports variable brightness but no color.
        /// </summary>
        public const string Brightness = "brightness";
        /// <summary>
        /// Light supports tunable-white via color temperature.
        /// </summary>
        public const string ColorTemp = "color_temp";
        /// <summary>
        /// HS color (hue + saturation) mode.
        /// </summary>
        public const string Hs = "hs";
        /// <summary>
        /// RGB color mode.
        /// </summary>
        public const string Rgb = "rgb";
        /// <summary>
        /// RGB color plus a dedicated white channel.
        /// </summary>
        public const string Rgbw = "rgbw";
        /// <summary>
        /// RGB color plus warm and cool white channels.
        /// </summary>
        public const string Rgbww = "rgbww";
        /// <summary>
        /// Dedicated white channel only.
        /// </summary>
        public const string White = "white";
        /// <summary>
        /// CIE xy chromaticity color mode.
        /// </summary>
        public const string Xy = "xy";
        /// <summary>
        /// Color-capable mode strings (everything that supports HS / RGB / XY).
        /// </summary>
        public static readonly string[] ChromaticModes = [Hs, Rgb, Rgbw, Rgbww, Xy];
    }

    /// <summary>
    /// HA <c>cover.supported_features</c> bit positions
    /// (<see href="https://developers.home-assistant.io/docs/core/entity/cover/" />).
    /// </summary>
    public static class CoverFeatures
    {
        /// <summary>
        /// Cover can be opened.
        /// </summary>
        public const int Open = 1;
        /// <summary>
        /// Cover can be closed.
        /// </summary>
        public const int Close = 2;
        /// <summary>
        /// Cover supports setting an absolute position.
        /// </summary>
        public const int SetPosition = 4;
        /// <summary>
        /// Cover supports stopping mid-travel.
        /// </summary>
        public const int Stop = 8;
        /// <summary>
        /// Cover supports opening its tilt.
        /// </summary>
        public const int OpenTilt = 16;
        /// <summary>
        /// Cover supports closing its tilt.
        /// </summary>
        public const int CloseTilt = 32;
        /// <summary>
        /// Cover supports stopping a tilt motion.
        /// </summary>
        public const int StopTilt = 64;
        /// <summary>
        /// Cover supports setting an absolute tilt position.
        /// </summary>
        public const int SetTiltPosition = 128;
    }

    /// <summary>
    /// HA <c>cover.device_class</c> values that distinguish what kind of cover we're
    /// looking at. Used to pick a single Alexa display category per cover.
    /// </summary>
    public static class CoverDeviceClasses
    {
        /// <summary>
        /// An exterior shading awning.
        /// </summary>
        public const string Awning = "awning";
        /// <summary>
        /// Slatted window blinds.
        /// </summary>
        public const string Blind = "blind";
        /// <summary>
        /// Soft fabric window curtains.
        /// </summary>
        public const string Curtain = "curtain";
        /// <summary>
        /// HVAC damper.
        /// </summary>
        public const string Damper = "damper";
        /// <summary>
        /// Interior door.
        /// </summary>
        public const string Door = "door";
        /// <summary>
        /// Garage door.
        /// </summary>
        public const string Garage = "garage";
        /// <summary>
        /// Driveway / property gate.
        /// </summary>
        public const string Gate = "gate";
        /// <summary>
        /// Roller / pleated window shade.
        /// </summary>
        public const string Shade = "shade";
        /// <summary>
        /// Exterior shutter.
        /// </summary>
        public const string Shutter = "shutter";
        /// <summary>
        /// Operable window.
        /// </summary>
        public const string Window = "window";
    }

    /// <summary>
    /// HA <c>fan.supported_features</c> bit positions.
    /// </summary>
    public static class FanFeatures
    {
        /// <summary>
        /// Fan supports a variable-speed control.
        /// </summary>
        public const int SetSpeed = 1;
        /// <summary>
        /// Fan supports oscillation control.
        /// </summary>
        public const int Oscillate = 2;
        /// <summary>
        /// Fan supports a forward/reverse direction control.
        /// </summary>
        public const int Direction = 4;
        /// <summary>
        /// Fan supports preset speed modes.
        /// </summary>
        public const int PresetMode = 8;
        /// <summary>
        /// Fan supports an explicit turn-on service call.
        /// </summary>
        public const int TurnOn = 16;
        /// <summary>
        /// Fan supports an explicit turn-off service call.
        /// </summary>
        public const int TurnOff = 32;
    }

    /// <summary>
    /// HA <c>climate.supported_features</c> bit positions.
    /// </summary>
    public static class ClimateFeatures
    {
        /// <summary>
        /// Climate device supports a single setpoint.
        /// </summary>
        public const int TargetTemperature = 1;
        /// <summary>
        /// Climate device supports a low/high setpoint range.
        /// </summary>
        public const int TargetTemperatureRange = 2;
        /// <summary>
        /// Climate device supports a humidity setpoint.
        /// </summary>
        public const int TargetHumidity = 4;
        /// <summary>
        /// Climate device supports selectable fan modes.
        /// </summary>
        public const int FanMode = 8;
        /// <summary>
        /// Climate device supports named preset modes.
        /// </summary>
        public const int PresetMode = 16;
        /// <summary>
        /// Climate device supports swing-direction modes.
        /// </summary>
        public const int SwingMode = 32;
        /// <summary>
        /// Climate device supports auxiliary heating.
        /// </summary>
        public const int AuxHeat = 64;
        /// <summary>
        /// Climate device supports an explicit turn-off service call.
        /// </summary>
        public const int TurnOff = 128;
        /// <summary>
        /// Climate device supports an explicit turn-on service call.
        /// </summary>
        public const int TurnOn = 256;
    }

    /// <summary>
    /// Common HA HVAC mode strings.
    /// </summary>
    public static class HvacModes
    {
        /// <summary>
        /// HVAC off.
        /// </summary>
        public const string Off = "off";
        /// <summary>
        /// Heat-only mode.
        /// </summary>
        public const string Heat = "heat";
        /// <summary>
        /// Cool-only mode.
        /// </summary>
        public const string Cool = "cool";
        /// <summary>
        /// Dual-setpoint heat/cool mode.
        /// </summary>
        public const string HeatCool = "heat_cool";
        /// <summary>
        /// Auto / programmable mode (depends on device).
        /// </summary>
        public const string Auto = "auto";
        /// <summary>
        /// Dehumidify-only mode.
        /// </summary>
        public const string Dry = "dry";
        /// <summary>
        /// Fan-only circulation.
        /// </summary>
        public const string FanOnly = "fan_only";
    }

    /// <summary>
    /// HA <c>media_player.supported_features</c> bit positions
    /// (<see href="https://developers.home-assistant.io/docs/core/entity/media-player/" />).
    /// </summary>
    public static class MediaPlayerFeatures
    {
        /// <summary>
        /// Media player supports pause.
        /// </summary>
        public const int Pause = 1 << 0;
        /// <summary>
        /// Media player supports seeking within a track.
        /// </summary>
        public const int Seek = 1 << 1;
        /// <summary>
        /// Media player supports absolute volume control.
        /// </summary>
        public const int VolumeSet = 1 << 2;
        /// <summary>
        /// Media player supports a mute toggle.
        /// </summary>
        public const int VolumeMute = 1 << 3;
        /// <summary>
        /// Media player supports skipping to the previous track.
        /// </summary>
        public const int PreviousTrack = 1 << 4;
        /// <summary>
        /// Media player supports skipping to the next track.
        /// </summary>
        public const int NextTrack = 1 << 5;
        /// <summary>
        /// Media player supports an explicit turn-on service call.
        /// </summary>
        public const int TurnOn = 1 << 6;
        /// <summary>
        /// Media player supports an explicit turn-off service call.
        /// </summary>
        public const int TurnOff = 1 << 7;
        /// <summary>
        /// Media player supports playing arbitrary media.
        /// </summary>
        public const int PlayMedia = 1 << 8;
        /// <summary>
        /// Media player supports step-wise volume adjustment.
        /// </summary>
        public const int VolumeStep = 1 << 9;
        /// <summary>
        /// Media player supports selecting an input source.
        /// </summary>
        public const int SelectSource = 1 << 10;
        /// <summary>
        /// Media player supports stop.
        /// </summary>
        public const int Stop = 1 << 11;
        /// <summary>
        /// Media player supports clearing its playlist.
        /// </summary>
        public const int ClearPlaylist = 1 << 12;
        /// <summary>
        /// Media player supports starting playback.
        /// </summary>
        public const int Play = 1 << 13;
        /// <summary>
        /// Media player supports toggling shuffle mode.
        /// </summary>
        public const int ShuffleSet = 1 << 14;
        /// <summary>
        /// Media player supports selecting a sound mode.
        /// </summary>
        public const int SelectSoundMode = 1 << 15;
        /// <summary>
        /// Media player supports media browsing.
        /// </summary>
        public const int BrowseMedia = 1 << 16;
        /// <summary>
        /// Media player supports configuring repeat mode.
        /// </summary>
        public const int RepeatSet = 1 << 17;
        /// <summary>
        /// Media player supports multi-room grouping.
        /// </summary>
        public const int Grouping = 1 << 18;
    }

    /// <summary>
    /// HA <c>media_player.device_class</c> values used to pick a single Alexa display
    /// category. Most user-facing media players fall into TV or Speaker; receivers
    /// don't have a perfect Alexa equivalent and get Speaker as the closest fit.
    /// </summary>
    public static class MediaPlayerDeviceClasses
    {
        /// <summary>
        /// Television.
        /// </summary>
        public const string Tv = "tv";
        /// <summary>
        /// Speaker / smart speaker / soundbar.
        /// </summary>
        public const string Speaker = "speaker";
        /// <summary>
        /// AV receiver — surfaced as a Speaker to Alexa.
        /// </summary>
        public const string Receiver = "receiver";
    }

    /// <summary>
    /// HA <c>sensor.device_class</c> values relevant to the bridge. Only the subset
    /// that maps to an Alexa sensor interface is listed; other device classes are not
    /// discoverable through this bridge.
    /// </summary>
    public static class SensorDeviceClasses
    {
        /// <summary>
        /// Temperature sensor (reported in Celsius or Fahrenheit per HA's unit).
        /// </summary>
        public const string Temperature = "temperature";
        /// <summary>
        /// Relative-humidity sensor (percent).
        /// </summary>
        public const string Humidity = "humidity";
    }
}
