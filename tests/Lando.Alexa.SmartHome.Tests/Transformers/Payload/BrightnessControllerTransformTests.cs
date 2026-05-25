using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// BrightnessController transforms: Set (absolute 0..100) and Adjust
/// (relative −100..100 step). Both translate to <c>light.turn_on</c> in HA;
/// SetBrightness passes the absolute via <c>brightness_pct</c>, AdjustBrightness
/// passes the delta via <c>brightness_step_pct</c> — which lets HA do the
/// current-state read on its side and avoids a round-trip.
/// </summary>
public class BrightnessControllerTransformTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void SetBrightness_emits_turn_on_with_absolute_brightness_pct(int brightness)
    {
        var entity = TestEntities.Light();
        var request = new SetBrightnessPayloadTransform().Transform(entity, new SetBrightnessPayload { Brightness = brightness });

        request.Service.ShouldBe("turn_on");
        request.EntityId.ShouldBe(entity.EntityId);
        request.Brightness.ShouldBe(brightness);
    }

    [Theory]
    [InlineData(-25)]
    [InlineData(10)]
    public void AdjustBrightness_emits_turn_on_with_brightness_step_pct(int delta)
    {
        var entity = TestEntities.Light();
        var request = new AdjustBrightnessPayloadTransform().Transform(entity, new AdjustBrightnessPayload { BrightnessDelta = delta });

        request.Service.ShouldBe("turn_on");
        request.BrightnessStepPercent.ShouldBe(delta);
    }
}
