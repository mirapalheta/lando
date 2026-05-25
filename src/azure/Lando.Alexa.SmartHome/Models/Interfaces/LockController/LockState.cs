namespace Lando.Alexa.SmartHome.Models.Interfaces.LockController;

/// <summary>
/// Property values for <c>Alexa.LockController.lockState</c>. JAMMED is the failure state
/// the bridge reports when the motor stalls — it's a normal property value, not an error.
/// </summary>
public static class LockState
{
    public const string Locked = "LOCKED";
    public const string Unlocked = "UNLOCKED";
    public const string Jammed = "JAMMED";
}
