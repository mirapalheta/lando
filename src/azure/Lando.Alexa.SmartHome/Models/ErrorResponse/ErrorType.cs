using System.ComponentModel;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// Canonical Alexa error <c>type</c> values used on <c>Alexa.ErrorResponse</c> events.
/// </summary>
/// <remarks>
/// These are strings on the wire — the bridge sends them verbatim. Grouped roughly by
/// foundational vs. interface-specific to make the file easier to scan.
/// </remarks>
public enum ErrorType
{
    // ---------- Generic ----------
    [Description("ALREADY_IN_OPERATION")]
    AlreadyInOperation,
    [Description("BRIDGE_UNREACHABLE")]
    BridgeUnreachable,
    [Description("CLOUD_CONTROL_DISABLED")]
    CloudControlDisabled,
    [Description("ENDPOINT_BUSY")]
    EndpointBusy,
    [Description("ENDPOINT_LOW_POWER")]
    EndpointLowPower,
    [Description("ENDPOINT_UNREACHABLE")]
    EndpointUnreachable,
    [Description("EXPIRED_AUTHORIZATION_CREDENTIAL")]
    ExpiredAuthorizationCredential,
    [Description("FIRMWARE_OUT_OF_DATE")]
    FirmwareOutOfDate,
    [Description("HARDWARE_MALFUNCTION")]
    HardwareMalfunction,
    [Description("INSUFFICIENT_PERMISSIONS")]
    InsufficientPermissions,
    [Description("INTERNAL_ERROR")]
    InternalError,
    [Description("INVALID_AUTHORIZATION_CREDENTIAL")]
    InvalidAuthorizationCredential,
    [Description("INVALID_DIRECTIVE")]
    InvalidDirective,
    [Description("INVALID_VALUE")]
    InvalidValue,
    [Description("NO_SUCH_ENDPOINT")]
    NoSuchEndpoint,
    [Description("NOT_CALIBRATED")]
    NotCalibrated,
    [Description("NOT_SUPPORTED_IN_CURRENT_MODE")]
    NotSupportedInCurrentMode,
    [Description("NOT_IN_OPERATION")]
    NotInOperation,
    [Description("POWER_LEVEL_NOT_SUPPORTED")]
    PowerLevelNotSupported,
    [Description("RATE_LIMIT_EXCEEDED")]
    RateLimitExceeded,
    [Description("TEMPERATURE_VALUE_OUT_OF_RANGE")]
    TemperatureValueOutOfRange,
    [Description("VALUE_OUT_OF_RANGE")]
    ValueOutOfRange,
    [Description("TOO_MANY_FAILED_ATTEMPTS")]
    TooManyFailedAttempts,

    // ---------- Authorization ----------
    [Description("ACCEPT_GRANT_FAILED")]
    AcceptGrantFailed,

    // ---------- ThermostatController ----------
    [Description("DUAL_SETPOINTS_TOO_CLOSE")]
    DualSetpointsTooClose,
    [Description("REQUESTED_SETPOINT_TOO_HIGH")]
    RequestedSetpointTooHigh,
    [Description("REQUESTED_SETPOINT_TOO_LOW")]
    RequestedSetpointTooLow,
    [Description("TRIGGER_THRESHOLD_OUT_OF_RANGE")]
    TriggerThresholdOutOfRange,
    [Description("UNSUPPORTED_THERMOSTAT_MODE")]
    UnsupportedThermostatMode,

    // ---------- SecurityPanelController ----------
    [Description("AUTHORIZATION_REQUIRED")]
    AuthorizationRequired,
    [Description("NOT_READY")]
    NotReady,
    [Description("UNAUTHORIZED")]
    UnauthorizedUser,
    [Description("UNCLEARED_ALARM")]
    UnclearedAlarm,
    [Description("UNCLEARED_TROUBLE")]
    UnclearedTrouble,

    // ---------- Video ----------
    [Description("CONTENT_DURATION_LIMIT_EXCEEDED")]
    ContentDurationLimitExceeded,
    [Description("NOT_SUBSCRIBED")]
    NotSubscribed
}
