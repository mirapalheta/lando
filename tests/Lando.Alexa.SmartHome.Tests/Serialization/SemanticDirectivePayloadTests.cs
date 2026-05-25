using System.Linq;
using System.Text.Json;
using Lando.Alexa.SmartHome.Models.Discovery;

namespace Lando.Alexa.SmartHome.Serialization.Tests;

/// <summary>
/// <see cref="SemanticDirective.Payload"/> is declared as <see cref="object"/>, so
/// correct serialization depends on System.Text.Json walking the runtime type at write
/// time. If polymorphism breaks (e.g. a future framework upgrade flips the default to
/// declared-type), these mappings would silently emit <c>"payload": {}</c> — the action
/// mapping would still parse on Alexa's side but the rangeValue/rangeValueDelta would
/// be missing, so "open the blinds" would no longer translate into 100% and "speed up
/// the fan" would no longer adjust by 33.
/// </summary>
/// <remarks>
/// These tests serialize the static capability definitions and assert on the resulting
/// JSON structure, which is exactly what Alexa receives at discovery time. Cheap to
/// run and the only way to catch this class of regression without a live skill test.
/// </remarks>
public class SemanticDirectivePayloadTests
{
    [Fact]
    public void Shade_open_action_serializes_rangeValue_100()
    {
        var open = ActionMappingFor(Capability.ShadePosition, "Alexa.Actions.Open");
        var payload = open.GetProperty("directive").GetProperty("payload");

        payload.GetProperty("rangeValue").GetInt32().ShouldBe(100);
    }

    [Fact]
    public void Shade_close_action_serializes_rangeValue_0()
    {
        var close = ActionMappingFor(Capability.ShadePosition, "Alexa.Actions.Close");
        var payload = close.GetProperty("directive").GetProperty("payload");

        payload.GetProperty("rangeValue").GetInt32().ShouldBe(0);
    }

    [Fact]
    public void Fan_raise_action_serializes_rangeValueDelta_positive_33()
    {
        var raise = ActionMappingFor(Capability.FanSpeed, "Alexa.Actions.Raise");
        var payload = raise.GetProperty("directive").GetProperty("payload");

        payload.GetProperty("rangeValueDelta").GetInt32().ShouldBe(33);
    }

    [Fact]
    public void Fan_lower_action_serializes_rangeValueDelta_negative_33()
    {
        var lower = ActionMappingFor(Capability.FanSpeed, "Alexa.Actions.Lower");
        var payload = lower.GetProperty("directive").GetProperty("payload");

        payload.GetProperty("rangeValueDelta").GetInt32().ShouldBe(-33);
    }

    [Fact]
    public void Shade_state_mappings_emit_StatesToValue_and_StatesToRange()
    {
        var json = JsonSerializer.SerializeToDocument(Capability.ShadePosition);
        var states = json.RootElement
            .GetProperty("semantics")
            .GetProperty("stateMappings")
            .EnumerateArray()
            .ToList();

        states.ShouldContain(s => s.GetProperty("@type").GetString() == "StatesToValue");
        states.ShouldContain(s => s.GetProperty("@type").GetString() == "StatesToRange");

        var closedMapping = states.First(s =>
            s.GetProperty("states").EnumerateArray().Any(a => a.GetString() == "Alexa.States.Closed"));
        closedMapping.GetProperty("value").GetInt32().ShouldBe(0);
    }

    [Fact]
    public void Shade_capability_resources_use_Alexa_Setting_Opening_asset()
    {
        var json = JsonSerializer.SerializeToDocument(Capability.ShadePosition);
        var assetId = json.RootElement
            .GetProperty("capabilityResources")
            .GetProperty("friendlyNames")
            .EnumerateArray()
            .First()
            .GetProperty("value")
            .GetProperty("assetId")
            .GetString();

        assetId.ShouldBe("Alexa.Setting.Opening");
    }

    /// <summary>
    /// Helper — serialize a capability and locate the action mapping that lists the
    /// requested Alexa action. Throws if no match (which is itself a test failure).
    /// </summary>
    private static JsonElement ActionMappingFor(Capability capability, string action)
    {
        var json = JsonSerializer.SerializeToDocument(capability);
        return json.RootElement
            .GetProperty("semantics")
            .GetProperty("actionMappings")
            .EnumerateArray()
            .First(m => m.GetProperty("actions").EnumerateArray().Any(a => a.GetString() == action));
    }
}
