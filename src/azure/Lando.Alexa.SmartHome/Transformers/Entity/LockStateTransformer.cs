using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.LockController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// State transformer for the <c>lock</c> HA domain. Reports
/// <c>Alexa.LockController.lockState</c> mapped from the HA entity state string.
/// </summary>
/// <remarks>
/// HA intermediate states (<c>locking</c>, <c>unlocking</c>) are reported as
/// <c>LOCKED</c> and <c>UNLOCKED</c> respectively — Alexa has no in-progress
/// state, so optimistic reporting matches the direction of travel.
/// The HA <c>jammed</c> state maps directly to <c>JAMMED</c>, which the Alexa
/// app surfaces as a fault notification.
/// </remarks>
public class LockStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        yield return new ContextProperty
        {
            Namespace = Namespaces.LockController,
            Name = LockControllerProperties.LockState,
            Value = MapLockState(entity.State),
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };
    }

    /// <summary>
    /// Maps HA lock state strings to their Alexa <c>LockController.lockState</c>
    /// equivalents.
    /// </summary>
    private static string MapLockState(string? state)
        => state switch
        {
            "locked" or "locking" => LockState.Locked,
            "jammed" => LockState.Jammed,
            _ => LockState.Unlocked
        };
}
