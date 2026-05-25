using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>media_player</c> HA domain. Always advertises
/// <c>Alexa.PowerController</c>; layers on <c>Alexa.Speaker</c> when the entity
/// supports the <c>VOLUME_SET</c> feature.
/// </summary>
/// <remarks>
/// PlaybackController, InputController, and ChannelController are deliberately
/// not advertised yet — playback and input behavior varies enough across HA
/// integrations that silent partial failures are likely. Power + volume is the
/// universal denominator for TVs and speakers.
/// </remarks>
public class MediaPlayerDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity)
        => entity.GetDeviceClass() switch
        {
            MediaPlayerDeviceClasses.Speaker => DisplayCategory.Speaker,
            MediaPlayerDeviceClasses.Receiver => DisplayCategory.Speaker,
            _ => DisplayCategory.Television
        };

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        yield return Capability.PowerController;

        if ((entity.GetSupportedFeatures() & MediaPlayerFeatures.VolumeSet) != 0)
            yield return Capability.Speaker;
    }
}
