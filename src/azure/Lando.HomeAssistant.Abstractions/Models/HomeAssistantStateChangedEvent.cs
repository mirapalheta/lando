namespace Lando.HomeAssistant.Models;

/// <summary>
/// Represents the data carried by a Home Assistant <c>state_changed</c> WebSocket event.
/// </summary>
public sealed class HomeAssistantStateChangedEvent
{
    /// <summary>
    /// The entity whose state changed (e.g. <c>"light.living_room"</c>).
    /// </summary>
    public string EntityId { get; set; } = null!;

    /// <summary>
    /// The new state of the entity. <see langword="null"/> when the entity was removed.
    /// </summary>
    public HomeAssistantEntity? NewState { get; set; }

    /// <summary>
    /// The old state of the entity. <see langword="null"/> when the entity was first added.
    /// </summary>
    public HomeAssistantEntity? OldState { get; set; }
}
