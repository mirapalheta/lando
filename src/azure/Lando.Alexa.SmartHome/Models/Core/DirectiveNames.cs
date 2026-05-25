namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// String constants for every inbound directive name this bridge understands.
/// </summary>
/// <remarks>
/// Names are scoped by their interface for readability. The interface namespace lives on
/// <see cref="DirectiveHeader.Namespace"/>; this class only holds the directive name itself.
/// </remarks>
public static class DirectiveNames
{
    // Foundational
    public const string ReportState = "ReportState";
    public const string AcceptGrant = "AcceptGrant";
    public const string Discover = "Discover";

    // PowerController
    public const string TurnOn = "TurnOn";
    public const string TurnOff = "TurnOff";

    // Brightness / Percentage / PowerLevel — share the same directive names
    public const string SetBrightness = "SetBrightness";
    public const string AdjustBrightness = "AdjustBrightness";
    public const string SetPercentage = "SetPercentage";
    public const string AdjustPercentage = "AdjustPercentage";
    public const string SetPowerLevel = "SetPowerLevel";
    public const string AdjustPowerLevel = "AdjustPowerLevel";

    // ColorController
    public const string SetColor = "SetColor";

    // ColorTemperatureController
    public const string SetColorTemperature = "SetColorTemperature";
    public const string IncreaseColorTemperature = "IncreaseColorTemperature";
    public const string DecreaseColorTemperature = "DecreaseColorTemperature";

    // ThermostatController
    public const string SetTargetTemperature = "SetTargetTemperature";
    public const string AdjustTargetTemperature = "AdjustTargetTemperature";
    public const string SetThermostatMode = "SetThermostatMode";
    public const string ResumeSchedule = "ResumeSchedule";

    // LockController
    public const string Lock = "Lock";
    public const string Unlock = "Unlock";

    // Speaker / StepSpeaker
    public const string SetVolume = "SetVolume";
    public const string AdjustVolume = "AdjustVolume";
    public const string SetMute = "SetMute";

    // ChannelController
    public const string ChangeChannel = "ChangeChannel";
    public const string SkipChannels = "SkipChannels";

    // InputController
    public const string SelectInput = "SelectInput";

    // SceneController
    public const string Activate = "Activate";
    public const string Deactivate = "Deactivate";

    // ModeController
    public const string SetMode = "SetMode";
    public const string AdjustMode = "AdjustMode";

    // RangeController
    public const string SetRangeValue = "SetRangeValue";
    public const string AdjustRangeValue = "AdjustRangeValue";

    // ToggleController
    // (re-uses TurnOn / TurnOff)

    // EqualizerController
    public const string SetBands = "SetBands";
    public const string AdjustBands = "AdjustBands";
    public const string ResetBands = "ResetBands";
    public const string SetEqualizerMode = "SetMode";

    // PlaybackController
    public const string Play = "Play";
    public const string Pause = "Pause";
    public const string Stop = "Stop";
    public const string StartOver = "StartOver";
    public const string Previous = "Previous";
    public const string Next = "Next";
    public const string Rewind = "Rewind";
    public const string FastForward = "FastForward";

    // CameraStreamController
    public const string InitializeCameraStreams = "InitializeCameraStreams";

    // SecurityPanelController
    public const string Arm = "Arm";
    public const string Disarm = "Disarm";

    // WakeOnLANController
    public const string WakeUp = "WakeUp";

    // TimeHoldController
    public const string Hold = "Hold";
    public const string Resume = "Resume";
}
