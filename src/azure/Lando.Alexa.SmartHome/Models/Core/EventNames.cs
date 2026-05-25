namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// String constants for outbound event names the bridge can emit on Alexa's event bus
/// or as synchronous responses.
/// </summary>
public static class EventNames
{
    public const string Response = "Response";
    public const string StateReport = "StateReport";
    public const string ChangeReport = "ChangeReport";
    public const string DeferredResponse = "DeferredResponse";
    public const string ErrorResponse = "ErrorResponse";

    public const string DiscoverResponse = "Discover.Response";
    public const string AddOrUpdateReport = "AddOrUpdateReport";
    public const string DeleteReport = "DeleteReport";

    public const string AcceptGrantResponse = "AcceptGrant.Response";
    public const string AcceptGrantErrorResponse = "ErrorResponse";

    // SceneController — the response names are specific
    public const string ActivationStarted = "ActivationStarted";
    public const string DeactivationStarted = "DeactivationStarted";

    // DoorbellEventSource
    public const string DoorbellPress = "DoorbellPress";
}
