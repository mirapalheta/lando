using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>cover</c> HA domain. Splits covers into two
/// modes: positionable shade-likes (blinds, shades, shutters, curtains, awnings,
/// windows, and the unclassified default) get only <c>Alexa.RangeController</c>
/// with the shade-style semantic mappings, while binary covers (doors, garages,
/// gates) get <c>Alexa.PowerController</c>.
/// </summary>
/// <remarks>
/// The split is what produces the vertical position slider in the Alexa app
/// versus the open/close pill — both surface the right HA service call but render
/// differently. Position-less shade-likes fall back to PowerController too, so a
/// blind without <c>SET_POSITION</c> still gets a sensible UI.
/// </remarks>
public class CoverDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity)
        => entity.GetDeviceClass() switch
        {
            CoverDeviceClasses.Awning => DisplayCategory.Awning,
            CoverDeviceClasses.Curtain => DisplayCategory.Curtain,
            CoverDeviceClasses.Door => DisplayCategory.Door,
            CoverDeviceClasses.Garage or CoverDeviceClasses.Gate => DisplayCategory.GarageDoor,
            _ => DisplayCategory.InteriorBlind
        };

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        var hasSetPosition = (entity.GetSupportedFeatures() & CoverFeatures.SetPosition) != 0;
        if (hasSetPosition && IsShadeLike(entity.GetDeviceClass()))
        {
            yield return Capability.ShadePosition;
            yield break;
        }

        // Open/close-only covers (doors, garages, gates, shade-likes without
        // SET_POSITION) keep the PowerController surface.
        yield return Capability.PowerController;
    }

    /// <summary>
    /// True for cover device classes that should render with the shade-style
    /// position slider when position control is supported. Doors, garages, and
    /// gates deliberately fall outside this set — they're binary devices.
    /// </summary>
    /// <remarks>
    /// Shared shape with <see cref="CoverStateTransformer"/> so discovery and state
    /// reporting agree on which capability surface each entity exposes; drift
    /// between the two would make Alexa flag the endpoint unhealthy.
    /// </remarks>
    /// <param name="deviceClass">The lower-cased HA cover device class.</param>
    /// <returns>True when the cover should render as a shade.</returns>
    internal static bool IsShadeLike(string? deviceClass)
        => deviceClass switch
        {
            CoverDeviceClasses.Blind
                or CoverDeviceClasses.Shade
                or CoverDeviceClasses.Shutter
                or CoverDeviceClasses.Curtain
                or CoverDeviceClasses.Awning
                or CoverDeviceClasses.Window
                or null => true,
            _ => false
        };
}
