using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;
using Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;
using Lando.Alexa.SmartHome.Models.Interfaces.HumiditySensor;
using Lando.Alexa.SmartHome.Models.Interfaces.LockController;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.Alexa.SmartHome.Models.Interfaces.TemperatureSensor;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// A capability advertised on a discovered endpoint. Encodes the interface, version, what
/// properties it exposes, friendly names, configuration, and optional semantic mappings.
/// </summary>
/// <remarks>
/// The older <c>SmartHomeCapability</c> covered the minimal shape used by hand-rolled
/// discovery responses; <see cref="Capability"/> is the full, schema-faithful form that
/// matches what Alexa returns in its own examples.
/// </remarks>
public sealed class Capability
{
    private const string StandardType = "AlexaInterface";
    private const string StandardVersion = PayloadVersion.V3;

    [JsonPropertyName("type")]
    public string Type { get; set; } = StandardType;

    [JsonPropertyName("interface")]
    public string Interface { get; set; } = string.Empty;

    /// <summary>
    /// Instance id for capabilities that can be configured multiple times per endpoint..
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = StandardVersion;

    [JsonPropertyName("properties")]
    public CapabilityProperties? Properties { get; set; }

    [JsonPropertyName("capabilityResources")]
    public CapabilityResources? CapabilityResources { get; set; }

    [JsonPropertyName("configuration")]
    public CapabilityConfiguration? Configuration { get; set; }

    [JsonPropertyName("semantics")]
    public CapabilitySemantics? Semantics { get; set; }

    /// <summary>
    /// <c>Alexa.SceneController</c> only: whether the scene/activity supports a
    /// <c>Deactivate</c> directive. Null (and therefore omitted) on every other
    /// capability.
    /// </summary>
    [JsonPropertyName("supportsDeactivation")]
    public bool? SupportsDeactivation { get; set; }

    /// <summary>
    /// <c>Alexa.SceneController</c> only: whether activation/deactivation is
    /// proactively reported to the Alexa event gateway. The bridge answers
    /// Activate/Deactivate synchronously, so this is reported <c>false</c>.
    /// Null (and therefore omitted) on every other capability.
    /// </summary>
    [JsonPropertyName("proactivelyReported")]
    public bool? ProactivelyReported { get; set; }

    /// <summary>
    /// Convenience factory for a stateless capability such as <c>Alexa.SceneController</c>
    /// or <c>Alexa.DoorbellEventSource</c> that exposes no <c>properties</c> block.
    /// </summary>
    public static Capability ForInterface(string interfaceName)
        => new() { Interface = interfaceName };

    /// <summary>
    /// Implicit conversion from string to <see cref="Capability"/> for simple cases where only the interface name is needed.
    /// Allows writing code like: <c>capabilities: [ "Alexa.PowerController" ]</c> instead of
    /// <c>capabilities: [ Capability.ForInterface("Alexa.PowerController") ]</c>.
    /// Note that this only sets the <c>interface</c> property; other properties like <c>type</c> and <c>version</c>
    /// will still be set to their default values, but properties like <c>properties</c>, <c>configuration</c>, and
    /// <c>semantics</c> will be left null, so this should only be used for simple capabilities that don't require those.
    /// For more complex capabilities, it's recommended to construct the full <see cref="Capability"/> object explicitly.
    /// </summary>
    /// <param name="interfaceName">The name of the Alexa interface this capability represents, e.g. "Alexa.PowerController".</param>
    /// <returns>A new <see cref="Capability"/> instance with the specified interface name and default values for other properties.</returns>
    /// <remarks>
    /// This implicit conversion is provided for convenience and readability in simple cases, but it may not be suitable for all scenarios.
    /// Developers should use their judgment to decide when it's appropriate to rely on this implicit conversion versus constructing the full <see cref="Capability"/> object explicitly.
    /// </remarks>
    public static implicit operator Capability(string interfaceName)
        => ForInterface(interfaceName);

    public static Capability Alexa { get; } = ForInterface(Namespaces.Alexa);

    // Architectural note on the flags below.
    //
    // - Retrievable:        true everywhere because ReportStateDirectiveHandler is registered
    //                       and can return current state on demand.
    // - ProactivelyReported: true everywhere because ChangeReportService subscribes to HA's
    //                       state_changed event for every exposed entity and posts to the
    //                       Alexa Event Gateway. Capabilities that don't set this flag have
    //                       their ChangeReports silently dropped by Alexa — they appear to
    //                       send (202 Accepted at the gateway) but never reach the customer's
    //                       endpoint, which manifests as "device isn't responding" in the app.
    // - NonControllable:    true on read-only sensor properties (TemperatureSensor /
    //                       HumiditySensor) so Alexa surfaces them as observation-only.

    public static Capability EndpointHealth { get; } = new()
    {
        Interface = Namespaces.EndpointHealth,
        Properties = new CapabilityProperties
        {
            Supported = [new(EndpointHealthProperties.Connectivity)]
        }
    };

    public static Capability PowerController { get; } = new()
    {
        Interface = Namespaces.PowerController,
        Properties = new CapabilityProperties
        {
            Supported = [new(PowerControllerProperties.PowerState)]
        }
    };

    public static Capability BrightnessController { get; } = new()
    {
        Interface = Namespaces.BrightnessController,
        Properties = new CapabilityProperties
        {
            Supported = [new(BrightnessControllerProperties.Brightness)]
        }
    };

    public static Capability ColorController { get; } = new()
    {
        Interface = Namespaces.ColorController,
        Properties = new CapabilityProperties
        {
            Supported = [new(ColorControllerProperties.Color)]
        }
    };

    public static Capability ColorTemperatureController { get; } = new()
    {
        Interface = Namespaces.ColorTemperatureController,
        Properties = new CapabilityProperties
        {
            Supported = [new(ColorTemperatureControllerProperties.ColorTemperatureInKelvin)]
        }
    };

    public static Capability PercentageController { get; } = new()
    {
        Interface = Namespaces.PercentageController,
        Properties = new CapabilityProperties
        {
            Supported = [new(PercentageControllerProperties.Percentage)]
        }
    };

    /// <summary>
    /// Instance id used on the <see cref="ShadePosition"/> RangeController capability and
    /// echoed back on outgoing <c>StateReport</c> properties for the same capability. Voice
    /// utterances like "open the blinds" route through this instance's semantic mappings.
    /// </summary>
    public const string ShadePositionInstance = "Shade.Position";

    /// <summary>
    /// The <c>Alexa.RangeController</c> capability shape Alexa expects for window-covering
    /// endpoints (blinds, shades, shutters, curtains, awnings, positionable windows).
    /// Advertising this — instead of <see cref="PowerController"/> + <see cref="PercentageController"/> —
    /// is what causes the Alexa app to render the shade-style vertical position slider with
    /// no power button. The semantic mappings translate "open"/"close"/"raise"/"lower"
    /// utterances into <c>SetRangeValue</c> / <c>AdjustRangeValue</c> directives so the
    /// natural verbs still work.
    /// </summary>
    /// <remarks>
    /// The <c>Alexa.Setting.Opening</c> asset id on <c>capabilityResources</c> is what cues
    /// the Alexa app's shade UI specifically; a plain text friendly name would still work
    /// functionally but render as the generic range slider.
    /// </remarks>
    public static Capability ShadePosition { get; } = new()
    {
        Interface = Namespaces.RangeController,
        Instance = ShadePositionInstance,
        Properties = new CapabilityProperties
        {
            Supported = [new(RangeControllerProperties.RangeValue)]
        },
        CapabilityResources = new CapabilityResources
        {
            FriendlyNames =
            [
                new FriendlyName
                {
                    Type = FriendlyNameType.Asset,
                    Value = new FriendlyNameValue { AssetId = "Alexa.Setting.Opening" }
                }
            ]
        },
        Configuration = new CapabilityConfiguration
        {
            SupportedRange = new SupportedRange
            {
                MinimumValue = 0,
                MaximumValue = 100,
                Precision = 1
            },
            UnitOfMeasure = Units.Percent
        },
        Semantics = new CapabilitySemantics
        {
            ActionMappings =
            [
                new ActionMapping
                {
                    Actions = ["Alexa.Actions.Close", "Alexa.Actions.Lower"],
                    Directive = new SemanticDirective
                    {
                        Name = DirectiveNames.SetRangeValue,
                        Payload = new SetRangeValuePayload { RangeValue = 0 }
                    }
                },
                new ActionMapping
                {
                    Actions = ["Alexa.Actions.Open", "Alexa.Actions.Raise"],
                    Directive = new SemanticDirective
                    {
                        Name = DirectiveNames.SetRangeValue,
                        Payload = new SetRangeValuePayload { RangeValue = 100 }
                    }
                }
            ],
            StateMappings =
            [
                new StateMapping
                {
                    Type = "StatesToValue",
                    States = ["Alexa.States.Closed"],
                    Value = 0
                },
                new StateMapping
                {
                    Type = "StatesToRange",
                    States = ["Alexa.States.Open"],
                    Range = new SupportedRange { MinimumValue = 1, MaximumValue = 100 }
                }
            ]
        }
    };

    /// <summary>
    /// Instance id used on the <see cref="FanSpeed"/> RangeController capability and
    /// echoed back on outgoing <c>StateReport</c> properties. Voice utterances like
    /// "set the fan to high" route through this instance's semantic mappings + presets.
    /// </summary>
    public const string FanSpeedInstance = "Fan.Speed";

    /// <summary>
    /// <c>Alexa.RangeController</c> for fan speed. Presets at low/medium/high let voice
    /// utterances like "set the fan to medium" map onto specific percent values without
    /// the customer having to think in numbers, and the Raise/Lower semantics give them
    /// "speed up the fan" style commands too.
    /// </summary>
    /// <remarks>
    /// Replaces the older <see cref="PercentageController"/> shape on fan discovery.
    /// PercentageController still works fine functionally, but Amazon's modern guidance
    /// is to use RangeController with presets + semantics for any speed/level surface,
    /// which yields better voice utterance coverage and a more consistent app render.
    /// </remarks>
    public static Capability FanSpeed { get; } = new()
    {
        Interface = Namespaces.RangeController,
        Instance = FanSpeedInstance,
        Properties = new CapabilityProperties
        {
            Supported = [new(RangeControllerProperties.RangeValue)]
        },
        CapabilityResources = new CapabilityResources
        {
            FriendlyNames =
            [
                new FriendlyName
                {
                    Type = FriendlyNameType.Asset,
                    Value = new FriendlyNameValue { AssetId = "Alexa.Setting.FanSpeed" }
                }
            ]
        },
        Configuration = new CapabilityConfiguration
        {
            SupportedRange = new SupportedRange
            {
                MinimumValue = 0,
                MaximumValue = 100,
                Precision = 1
            },
            UnitOfMeasure = Units.Percent,
            Presets =
            [
                PresetWithAsset(0, "Alexa.Value.Minimum"),
                PresetWithAsset(33, "Alexa.Value.Low"),
                PresetWithAsset(66, "Alexa.Value.Medium"),
                PresetWithAsset(100, "Alexa.Value.High"),
                PresetWithAsset(100, "Alexa.Value.Maximum")
            ]
        },
        Semantics = new CapabilitySemantics
        {
            ActionMappings =
            [
                new ActionMapping
                {
                    Actions = ["Alexa.Actions.Lower"],
                    Directive = new SemanticDirective
                    {
                        Name = DirectiveNames.AdjustRangeValue,
                        Payload = new AdjustRangeValuePayload { RangeValueDelta = -33, RangeValueDeltaDefault = false }
                    }
                },
                new ActionMapping
                {
                    Actions = ["Alexa.Actions.Raise"],
                    Directive = new SemanticDirective
                    {
                        Name = DirectiveNames.AdjustRangeValue,
                        Payload = new AdjustRangeValuePayload { RangeValueDelta = 33, RangeValueDeltaDefault = false }
                    }
                }
            ]
        }
    };

    private static Preset PresetWithAsset(double rangeValue, string assetId) => new()
    {
        RangeValue = rangeValue,
        PresetResources = new CapabilityResources
        {
            FriendlyNames =
            [
                new FriendlyName
                {
                    Type = FriendlyNameType.Asset,
                    Value = new FriendlyNameValue { AssetId = assetId }
                }
            ]
        }
    };

    /// <summary>
    /// <c>Alexa.Speaker</c>. Supports both absolute and relative volume changes plus
    /// muting — Alexa's app renders the volume slider, mic-mute toggle, and accepts
    /// "set the volume to X" / "raise the volume" / "mute" utterances against
    /// endpoints carrying this capability.
    /// </summary>
    /// <remarks>
    /// Volume on the wire is a 0..100 integer; HA reports 0.0..1.0. Conversion happens
    /// in the bridge handlers, not the model.
    /// </remarks>
    public static Capability Speaker { get; } = new()
    {
        Interface = Namespaces.Speaker,
        Properties = new CapabilityProperties
        {
            Supported =
            [
                new(SpeakerProperties.Volume),
                new(SpeakerProperties.Muted)
            ]
        }
    };

    public static Capability HumiditySensor { get; } = new()
    {
        Interface = Namespaces.HumiditySensor,
        Properties = new CapabilityProperties
        {
            Supported = [new(HumiditySensorProperties.RelativeHumidity)],
            NonControllable = true
        }
    };

    public static Capability TemperatureSensor { get; } = new()
    {
        Interface = Namespaces.TemperatureSensor,
        Properties = new CapabilityProperties
        {
            Supported = [new(TemperatureSensorProperties.Temperature)],
            NonControllable = true
        }
    };

    public static Capability LockController { get; } = new()
    {
        Interface = Namespaces.LockController,
        Properties = new CapabilityProperties
        {
            Supported = [new(LockControllerProperties.LockState)]
        }
    };

    /// <summary>
    /// Builds the <c>Alexa.SceneController</c> capability advertised on HA scene
    /// and script endpoints. <paramref name="supportsDeactivation"/> is true for
    /// scripts (stoppable via <c>script.turn_off</c>) and false for scenes
    /// (fire-only). Activate/Deactivate are answered synchronously, so
    /// <c>proactivelyReported</c> is false.
    /// </summary>
    /// <param name="supportsDeactivation">Whether the endpoint accepts Deactivate.</param>
    /// <returns>The SceneController capability for discovery.</returns>
    public static Capability SceneController(bool supportsDeactivation) => new()
    {
        Interface = Namespaces.SceneController,
        SupportsDeactivation = supportsDeactivation,
        ProactivelyReported = false
    };

    public static List<Capability> DefaultCapabilities => [Alexa, EndpointHealth];
}
