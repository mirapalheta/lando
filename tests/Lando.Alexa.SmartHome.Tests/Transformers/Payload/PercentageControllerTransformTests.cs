using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// PercentageController transforms: Set + Adjust. Covers route to
/// <c>set_cover_position</c>, fans to <c>set_percentage</c>, anything else is
/// rejected as <c>InvalidDirective</c>. The Adjust variant reads HA's current
/// value and clamps to 0..100 — these tests pin both the clamp and the
/// per-domain dispatch.
/// </summary>
public class PercentageControllerTransformTests
{
    [Fact]
    public void SetPercentage_on_cover_emits_set_cover_position()
    {
        var entity = TestEntities.Cover();
        var request = new SetPercentagePayloadTransform().Transform(entity, new SetPercentagePayload { Percentage = 42 });

        request.Service.ShouldBe("set_cover_position");
        request.Position.ShouldBe(42);
    }

    [Fact]
    public void SetPercentage_on_fan_emits_set_percentage()
    {
        var entity = TestEntities.Fan();
        var request = new SetPercentagePayloadTransform().Transform(entity, new SetPercentagePayload { Percentage = 33 });

        request.Service.ShouldBe("set_percentage");
        request.Percentage.ShouldBe(33);
    }

    [Fact]
    public void SetPercentage_on_unsupported_domain_throws_InvalidDirective()
    {
        var entity = TestEntities.Light();
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new SetPercentagePayloadTransform().Transform(entity, new SetPercentagePayload { Percentage = 50 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Theory]
    [InlineData(60, 30, 90)]       // 60 + 30 = 90, within bounds
    [InlineData(80, 30, 100)]      // clamped at 100
    [InlineData(10, -30, 0)]       // clamped at 0
    public void AdjustPercentage_on_cover_clamps_and_emits_set_cover_position(int current, int delta, int expected)
    {
        var entity = TestEntities.Cover(currentPosition: current);
        var request = new AdjustPercentagePayloadTransform().Transform(entity, new AdjustPercentagePayload { PercentageDelta = delta });

        request.Service.ShouldBe("set_cover_position");
        request.Position.ShouldBe(expected);
    }

    [Fact]
    public void AdjustPercentage_on_fan_emits_set_percentage()
    {
        var entity = TestEntities.Fan(percentage: 50);
        var request = new AdjustPercentagePayloadTransform().Transform(entity, new AdjustPercentagePayload { PercentageDelta = 25 });

        request.Service.ShouldBe("set_percentage");
        request.Percentage.ShouldBe(75);
    }

    [Fact]
    public void AdjustPercentage_on_unsupported_domain_throws_InvalidDirective()
    {
        var entity = TestEntities.Light();
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new AdjustPercentagePayloadTransform().Transform(entity, new AdjustPercentagePayload { PercentageDelta = 25 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }
}
