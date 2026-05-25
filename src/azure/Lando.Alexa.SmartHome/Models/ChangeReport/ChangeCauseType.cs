namespace Lando.Alexa.SmartHome.Models.ChangeReport;

/// <summary>
/// Why a property changed. Drives whether Alexa speaks confirmation to the user
/// (<see cref="VoiceInteraction"/>) or stays silent (<see cref="PhysicalInteraction"/>).
/// </summary>
public static class ChangeCauseType
{
    /// <summary>
    /// Customer flipped a physical switch, opened a door, etc..
    /// </summary>
    public const string PhysicalInteraction = "PHYSICAL_INTERACTION";

    /// <summary>
    /// Voice command processed via Alexa..
    /// </summary>
    public const string VoiceInteraction = "VOICE_INTERACTION";

    /// <summary>
    /// Customer interacted with the device's mobile/web app..
    /// </summary>
    public const string AppInteraction = "APP_INTERACTION";

    /// <summary>
    /// Periodic poll of the device returned a new value..
    /// </summary>
    public const string PeriodicPoll = "PERIODIC_POLL";

    /// <summary>
    /// Rule, scene, schedule, or automation in the device cloud fired..
    /// </summary>
    public const string RuleTrigger = "RULE_TRIGGER";
}
