using System;
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>light</c> HA domain. Reports PowerController plus
/// any of BrightnessController, ColorTemperatureController, and ColorController
/// whose backing HA attribute is present on the entity.
/// </summary>
/// <remarks>
/// Conversions:
/// <list type="bullet">
///   <item><description>brightness 0..255 (HA) → 0..100 (Alexa)</description></item>
///   <item><description>color_temp mired (HA) → kelvin (Alexa) = 1_000_000 / mired</description></item>
///   <item><description>hs_color [hue, sat%] (HA) → HSB with brightness 0..1 (Alexa)</description></item>
/// </list>
/// </remarks>
public class LightStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        var attrs = entity.Attributes;
        var brightness255 = attrs.GetInt(EntityAttributes.Brightness);

        yield return new ContextProperty
        {
            Namespace = Namespaces.PowerController,
            Name = PowerControllerProperties.PowerState,
            Value = entity.State == "on" ? PowerState.On : PowerState.Off,
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };

        if (brightness255 is int b)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.BrightnessController,
                Name = BrightnessControllerProperties.Brightness,
                Value = (int)Math.Round(b * 100.0 / 255.0),
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }

        var mired = attrs.GetInt(EntityAttributes.ColorTemp);
        if (mired is int m and > 0)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.ColorTemperatureController,
                Name = ColorTemperatureControllerProperties.ColorTemperatureInKelvin,
                Value = 1_000_000 / m,
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }

        var hsColor = attrs.GetStringArray("hs_color");
        if (hsColor is { Count: >= 2 }
            && double.TryParse(hsColor[0], out var hue)
            && double.TryParse(hsColor[1], out var saturationPercent))
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.ColorController,
                Name = ColorControllerProperties.Color,
                Value = new HsbColor
                {
                    Hue = hue,
                    Saturation = saturationPercent / 100.0,
                    Brightness = brightness255 is int br ? br / 255.0 : 1.0
                },
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }
    }
}
