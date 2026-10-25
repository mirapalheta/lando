using System;
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.PlaybackStateReporter;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>media_player</c> HA domain. Reports PowerController
/// for the on/off state, with paused / idle / standby all surfacing as ON because
/// users perceive a paused TV as still powered on. Reports PlaybackStateReporter
/// <c>playbackState</c> when the entity has any playback feature bit (i.e. discovery
/// advertised <c>Alexa.PlaybackController</c>). Reports Speaker volume and
/// muted when the entity supports <c>VOLUME_SET</c> (i.e. discovery advertised
/// <c>Alexa.Speaker</c>) and the matching HA attributes are present.
/// </summary>
/// <remarks>
/// HA stores volume as a 0.0..1.0 float; Alexa expects 0..100 integer percent.
/// Conversion happens here so the wire shape matches the Speaker spec without
/// each consumer needing to know HA's unit.
/// </remarks>
public class MediaPlayerStateTransformer : StateTransformerBase
{
    // The bits MediaPlayerDiscoveryTransformer maps to PlaybackController operations;
    // PlaybackStateReporter is advertised exactly when one of them is set.
    private const int PlaybackFeatures =
        MediaPlayerFeatures.Play | MediaPlayerFeatures.Pause | MediaPlayerFeatures.Stop
        | MediaPlayerFeatures.NextTrack | MediaPlayerFeatures.PreviousTrack;

    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        yield return new ContextProperty
        {
            Namespace = Namespaces.PowerController,
            Name = PowerControllerProperties.PowerState,
            Value = entity.State switch
            {
                "off" or "unavailable" or "unknown" or null => PowerState.Off,
                _ => PowerState.On
            },
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };

        var features = entity.GetSupportedFeatures();

        if ((features & PlaybackFeatures) != 0)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.PlaybackStateReporter,
                Name = PlaybackStateReporterProperties.PlaybackState,
                Value = new PlaybackState
                {
                    State = entity.State switch
                    {
                        "playing" or "buffering" => PlaybackStateValue.Playing,
                        "paused" => PlaybackStateValue.Paused,
                        _ => PlaybackStateValue.Stopped
                    }
                },
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }

        // Speaker properties only for endpoints that advertise Alexa.Speaker
        // (VOLUME_SET). Step-only players still report volume_level/is_volume_muted
        // in HA, but StepSpeaker has no properties, so reporting them would describe
        // an interface the endpoint never discovered.
        if ((features & MediaPlayerFeatures.VolumeSet) == 0)
            yield break;

        var attrs = entity.Attributes;

        if (attrs.GetDouble(EntityAttributes.VolumeLevel) is double level)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.Speaker,
                Name = SpeakerProperties.Volume,
                Value = (int)Math.Round(Math.Clamp(level, 0d, 1d) * 100d),
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }

        if (attrs.GetBool(EntityAttributes.IsVolumeMuted) is bool muted)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.Speaker,
                Name = SpeakerProperties.Muted,
                Value = muted,
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }
    }
}
