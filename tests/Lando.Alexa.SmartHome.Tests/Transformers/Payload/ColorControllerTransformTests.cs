using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// Color and ColorTemperature controller transforms. SetColor converts
/// Alexa's HSB (hue 0..360, saturation 0..1, brightness 0..1) into HA's
/// <c>hs_color</c> plus <c>brightness_pct</c> (0..100). The temperature
/// step handlers (Increase/Decrease) walk the current Kelvin by a fixed
/// step and clamp at the ends — these tests pin both the step math and the
/// clamps.
/// </summary>
public class ColorControllerTransformTests
{
    [Fact]
    public void SetColor_converts_HSB_to_HA_hs_color_and_brightness_pct()
    {
        var entity = TestEntities.Light();
        var payload = new SetColorPayload { Color = new HsbColor { Hue = 120, Saturation = 0.5, Brightness = 0.4 } };

        var request = new SetColorPayloadTransform().Transform(entity, payload);

        request.Service.ShouldBe("turn_on");
        request.HsColor.ShouldBe([120d, 50d]);
        request.Brightness.ShouldBe(40d);
    }

    [Fact]
    public void SetColorTemperature_forwards_kelvin_directly()
    {
        var entity = TestEntities.Light();
        var request = new SetColorTemperaturePayloadTransform()
            .Transform(entity, new SetColorTemperaturePayload { ColorTemperatureInKelvin = 3500 });

        request.Service.ShouldBe("turn_on");
        request.Kelvin.ShouldBe(3500);
    }

    [Fact]
    public void IncreaseColorTemperature_steps_up_500_kelvin_from_default_when_no_current()
    {
        var entity = TestEntities.Light();
        var request = new IncreaseColorTemperaturePayloadTransform().Transform(entity, EmptyPayload.Instance);
        // Default start 4000K + 500K = 4500K
        request.Kelvin.ShouldBe(4500);
    }

    [Fact]
    public void IncreaseColorTemperature_clamps_at_7000_kelvin()
    {
        // 6700K → mireds 149; +500 step caps at 7000K
        var entity = TestEntities.Light(colorTempMired: 149);
        var request = new IncreaseColorTemperaturePayloadTransform().Transform(entity, EmptyPayload.Instance);
        request.Kelvin.ShouldBe(7000);
    }

    [Fact]
    public void DecreaseColorTemperature_steps_down_500_kelvin_from_default()
    {
        var entity = TestEntities.Light();
        var request = new DecreaseColorTemperaturePayloadTransform().Transform(entity, EmptyPayload.Instance);
        request.Kelvin.ShouldBe(3500);
    }

    [Fact]
    public void DecreaseColorTemperature_floors_at_1900_kelvin()
    {
        // 2100K → mireds ~476; −500 step would underflow, floor at 1900K
        var entity = TestEntities.Light(colorTempMired: 476);
        var request = new DecreaseColorTemperaturePayloadTransform().Transform(entity, EmptyPayload.Instance);
        request.Kelvin.ShouldBe(1900);
    }
}
