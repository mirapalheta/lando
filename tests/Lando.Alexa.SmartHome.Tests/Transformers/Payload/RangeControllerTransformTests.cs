using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// RangeController transforms: Set + Adjust. Same routing as Percentage
/// (cover → set_cover_position, fan → set_percentage, else throw) but
/// payloads are doubles — these tests cover the round + clamp.
/// </summary>
public class RangeControllerTransformTests
{
    [Theory]
    [InlineData(42.0, 42)]
    [InlineData(42.4, 42)]
    [InlineData(42.6, 43)]
    [InlineData(-5.0, 0)]
    [InlineData(110.0, 100)]
    public void SetRangeValue_rounds_clamps_and_targets_cover(double input, int expected)
    {
        var entity = TestEntities.Cover();
        var request = new SetRangeValuePayloadTransform().Transform(entity, new SetRangeValuePayload { RangeValue = input });

        request.Service.ShouldBe("set_cover_position");
        request.Position.ShouldBe(expected);
    }

    [Fact]
    public void SetRangeValue_targets_fan()
    {
        var entity = TestEntities.Fan();
        var request = new SetRangeValuePayloadTransform().Transform(entity, new SetRangeValuePayload { RangeValue = 25.0 });

        request.Service.ShouldBe("set_percentage");
        request.Percentage.ShouldBe(25);
    }

    [Fact]
    public void SetRangeValue_throws_on_unsupported_domain()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new SetRangeValuePayloadTransform().Transform(TestEntities.Light(), new SetRangeValuePayload { RangeValue = 50 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Theory]
    [InlineData(60, 30.0, 90)]
    [InlineData(80, 30.0, 100)]
    [InlineData(10, -30.0, 0)]
    public void AdjustRangeValue_on_cover_clamps_and_emits_set_cover_position(int current, double delta, int expected)
    {
        var entity = TestEntities.Cover(currentPosition: current);
        var request = new AdjustRangeValuePayloadTransform().Transform(entity, new AdjustRangeValuePayload { RangeValueDelta = delta });

        request.Service.ShouldBe("set_cover_position");
        request.Position.ShouldBe(expected);
    }

    [Theory]
    [InlineData(50, 20.0, 70)]
    [InlineData(90, 20.0, 100)]
    [InlineData(5, -20.0, 0)]
    public void AdjustRangeValue_on_fan_clamps_and_emits_set_percentage(int current, double delta, int expected)
    {
        var entity = TestEntities.Fan(percentage: current);
        var request = new AdjustRangeValuePayloadTransform().Transform(entity, new AdjustRangeValuePayload { RangeValueDelta = delta });

        request.Service.ShouldBe("set_percentage");
        request.Percentage.ShouldBe(expected);
    }

    [Fact]
    public void AdjustRangeValue_throws_on_unsupported_domain()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new AdjustRangeValuePayloadTransform().Transform(TestEntities.Light(), new AdjustRangeValuePayload { RangeValueDelta = 10 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }
}
