using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>media_player</c> HA domain. Always advertises
/// <c>Alexa.PowerController</c>; layers on <c>Alexa.Speaker</c> when the entity
/// supports <c>VOLUME_SET</c>, otherwise <c>Alexa.StepSpeaker</c> when it supports
/// <c>VOLUME_STEP</c>, and <c>Alexa.PlaybackController</c> with one operation per
/// supported playback feature bit, paired with <c>Alexa.PlaybackStateReporter</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every capability is gated on <c>supported_features</c>, so Alexa is only ever
/// offered directives the integration says it can honour — the same mapping HA's
/// own <c>alexa</c> integration uses. Key-press integrations such as
/// <c>androidtv_remote</c> (step volume, play/pause, no absolute volume) get
/// StepSpeaker + PlaybackController; absolute-volume players keep Speaker.
/// </para>
/// <para>
/// InputController and ChannelController are still not advertised: HA's
/// <c>source_list</c> is missing or app-shaped on the integrations seen so far,
/// so input/channel directives would fail silently.
/// </para>
/// </remarks>
public class MediaPlayerDiscoveryTransformer : DiscoveryTransformerBase
{
    private static readonly (int Feature, string Operation)[] PlaybackOperations =
    [
        (MediaPlayerFeatures.Play, DirectiveNames.Play),
        (MediaPlayerFeatures.Pause, DirectiveNames.Pause),
        (MediaPlayerFeatures.Stop, DirectiveNames.Stop),
        (MediaPlayerFeatures.NextTrack, DirectiveNames.Next),
        (MediaPlayerFeatures.PreviousTrack, DirectiveNames.Previous)
    ];

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
        var features = entity.GetSupportedFeatures();

        yield return Capability.PowerController;

        if ((features & MediaPlayerFeatures.VolumeSet) != 0)
            yield return Capability.Speaker;
        else if ((features & MediaPlayerFeatures.VolumeStep) != 0)
            yield return Capability.StepSpeaker;

        var operations = new List<string>();
        foreach (var (feature, operation) in PlaybackOperations)
        {
            if ((features & feature) != 0)
                operations.Add(operation);
        }

        if (operations.Count > 0)
        {
            yield return Capability.PlaybackController(operations);

            // Alexa routes transport commands on reported playback state; without it,
            // Alexa+ treats the endpoint as "nothing playing" and never sends Pause/Next.
            yield return Capability.PlaybackStateReporter;
        }
    }
}
