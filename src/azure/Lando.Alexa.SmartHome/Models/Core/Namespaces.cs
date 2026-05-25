namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// String constants for every Alexa Smart Home interface namespace this bridge understands.
/// </summary>
/// <remarks>
/// Constants — not enums — because the wire format is the string. Centralizing them prevents
/// typos and gives a single point of truth when the Alexa team adds or renames an interface.
/// </remarks>
public static class Namespaces
{
    // ---------- Foundational ----------
    public const string Alexa = "Alexa";
    public const string Authorization = "Alexa.Authorization";
    public const string Discovery = "Alexa.Discovery";
    public const string EndpointHealth = "Alexa.EndpointHealth";

    // ---------- Smart home controllers ----------
    public const string BrightnessController = "Alexa.BrightnessController";
    public const string CameraStreamController = "Alexa.CameraStreamController";
    public const string ChannelController = "Alexa.ChannelController";
    public const string ColorController = "Alexa.ColorController";
    public const string ColorTemperatureController = "Alexa.ColorTemperatureController";
    public const string ContactSensor = "Alexa.ContactSensor";
    public const string DoorbellEventSource = "Alexa.DoorbellEventSource";
    public const string EqualizerController = "Alexa.EqualizerController";
    public const string HumiditySensor = "Alexa.HumiditySensor";
    public const string InputController = "Alexa.InputController";
    public const string LockController = "Alexa.LockController";
    public const string ModeController = "Alexa.ModeController";
    public const string MotionSensor = "Alexa.MotionSensor";
    public const string PercentageController = "Alexa.PercentageController";
    public const string PowerController = "Alexa.PowerController";
    public const string PowerLevelController = "Alexa.PowerLevelController";
    public const string PlaybackController = "Alexa.PlaybackController";
    public const string RangeController = "Alexa.RangeController";
    public const string SceneController = "Alexa.SceneController";
    public const string SecurityPanelController = "Alexa.SecurityPanelController";
    public const string Speaker = "Alexa.Speaker";
    public const string StepSpeaker = "Alexa.StepSpeaker";
    public const string TemperatureSensor = "Alexa.TemperatureSensor";
    public const string ThermostatController = "Alexa.ThermostatController";
    public const string TimeHoldController = "Alexa.TimeHoldController";
    public const string ToggleController = "Alexa.ToggleController";
    public const string WakeOnLANController = "Alexa.WakeOnLANController";
}
