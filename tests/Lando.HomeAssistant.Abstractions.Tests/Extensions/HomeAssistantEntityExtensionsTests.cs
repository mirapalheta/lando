using System.Collections.Generic;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant.Extensions.Tests;

using static Lando.HomeAssistant.Constants;

public class HomeAssistantEntityExtensionsTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static HomeAssistantEntity Entity(
        string entityId,
        Dictionary<string, object>? attrs = null) =>
        new() { EntityId = entityId, Attributes = attrs };

    // ── GetDomain (entity) ────────────────────────────────────────────────────

    [Theory]
    [InlineData("light.kitchen", "light")]
    [InlineData("binary_sensor.door", "binary_sensor")]
    [InlineData("climate.living_room", "climate")]
    public void GetDomain_EntityId_ExtractsDomainPart(string entityId, string expected)
    {
        Entity(entityId).GetDomain().ShouldBe(expected);
    }

    [Fact]
    public void GetDomain_EntityId_IsLowercased()
    {
        Entity("LIGHT.KITCHEN").GetDomain().ShouldBe("light");
    }

    [Fact]
    public void GetDomain_NoDotInEntityId_ReturnsWholeId()
    {
        Entity("nodomain").GetDomain().ShouldBe("nodomain");
    }

    [Fact]
    public void GetDomain_EmptyEntityId_ReturnsUnknown()
    {
        Entity("").GetDomain().ShouldBe(Domains.Unknown);
    }

    // ── GetDomain (HomeAssistantRequest static overload) ─────────────────────

    [Fact]
    public void GetDomain_NullRequest_ReturnsUnknown()
    {
        ((HomeAssistantRequest?)null).GetDomain().ShouldBe(Domains.Unknown);
    }

    [Fact]
    public void GetDomain_RequestWithEntityId_ExtractsDomain()
    {
        HomeAssistantRequest.TurnOn("switch.outlet").GetDomain().ShouldBe("switch");
    }

    // ── IsExposed ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsExposed_CustomAttributeTrue_ReturnsTrue()
    {
        var e = Entity("light.x", new() { ["my_expose"] = (object)true });
        e.IsExposed("my_expose").ShouldBeTrue();
    }

    [Fact]
    public void IsExposed_CustomAttributeFalse_ReturnsFalse_EvenIfLandoExposeTrue()
    {
        var e = Entity("light.x", new()
        {
            ["my_expose"] = (object)false,
            [CustomAttributes.Expose] = (object)true
        });
        e.IsExposed("my_expose").ShouldBeFalse();
    }

    [Fact]
    public void IsExposed_CustomAttributeMissing_FallsBackToLandoExpose()
    {
        var e = Entity("light.x", new() { [CustomAttributes.Expose] = (object)true });
        e.IsExposed("my_expose").ShouldBeTrue();
    }

    [Fact]
    public void IsExposed_NeitherAttributeSet_ReturnsFalse()
    {
        Entity("light.x").IsExposed("my_expose").ShouldBeFalse();
    }

    // ── GetFriendlyName ───────────────────────────────────────────────────────

    [Fact]
    public void GetFriendlyName_CustomAttributePresent_ReturnsIt()
    {
        var e = Entity("light.x", new() { ["alexa_name"] = (object)"My Light" });
        e.GetFriendlyName("alexa_name").ShouldBe("My Light");
    }

    [Fact]
    public void GetFriendlyName_CustomAttributeMissing_FallsBackToLandoName()
    {
        var e = Entity("light.x", new() { [CustomAttributes.Name] = (object)"Lando Name" });
        e.GetFriendlyName("alexa_name").ShouldBe("Lando Name");
    }

    [Fact]
    public void GetFriendlyName_LandoNameMissing_FallsBackToFriendlyName()
    {
        var e = Entity("light.x", new() { ["friendly_name"] = (object)"HA Name" });
        e.GetFriendlyName().ShouldBe("HA Name");
    }

    [Fact]
    public void GetFriendlyName_NoAttributesAtAll_FallsBackToEntityId()
    {
        Entity("light.kitchen").GetFriendlyName().ShouldBe("light.kitchen");
    }

    [Fact]
    public void GetFriendlyName_NoCustomAttribute_NullArgument_SkipsCustomLookup()
    {
        var e = Entity("light.x", new() { ["friendly_name"] = (object)"HA Name" });
        e.GetFriendlyName(null).ShouldBe("HA Name");
    }

    // ── GetDeviceClass ────────────────────────────────────────────────────────

    [Fact]
    public void GetDeviceClass_Present_ReturnsLowercased()
    {
        var e = Entity("cover.x", new() { ["device_class"] = (object)"Garage_Door" });
        e.GetDeviceClass().ShouldBe("garage_door");
    }

    [Fact]
    public void GetDeviceClass_Missing_ReturnsNull()
    {
        Entity("cover.x").GetDeviceClass().ShouldBeNull();
    }

    // ── GetSupportedFeatures ──────────────────────────────────────────────────

    [Fact]
    public void GetSupportedFeatures_Present_ReturnsBitmask()
    {
        var e = Entity("cover.x", new() { ["supported_features"] = (object)15 });
        e.GetSupportedFeatures().ShouldBe(15);
    }

    [Fact]
    public void GetSupportedFeatures_Missing_ReturnsZero()
    {
        Entity("cover.x").GetSupportedFeatures().ShouldBe(0);
    }

    // ── GetSupportedColorModes ────────────────────────────────────────────────

    [Fact]
    public void GetSupportedColorModes_Present_ReturnsList()
    {
        var json = System.Text.Json.JsonDocument.Parse("[\"color_temp\",\"hs\"]").RootElement.Clone();
        var e = Entity("light.x", new() { ["supported_color_modes"] = (object)json });
        e.GetSupportedColorModes().ShouldBe(["color_temp", "hs"]);
    }

    [Fact]
    public void GetSupportedColorModes_Missing_ReturnsEmptyList()
    {
        Entity("light.x").GetSupportedColorModes().ShouldBeEmpty();
    }

    // ── GetUnitOfMeasurement ──────────────────────────────────────────────────

    [Fact]
    public void GetUnitOfMeasurement_Present_ReturnsValue()
    {
        var e = Entity("sensor.temp", new() { ["unit_of_measurement"] = (object)"°C" });
        e.GetUnitOfMeasurement().ShouldBe("°C");
    }

    [Fact]
    public void GetUnitOfMeasurement_Missing_ReturnsNull()
    {
        Entity("sensor.temp").GetUnitOfMeasurement().ShouldBeNull();
    }

    // ── GetHvacModes ──────────────────────────────────────────────────────────

    [Fact]
    public void GetHvacModes_Present_ReturnsList()
    {
        var json = System.Text.Json.JsonDocument.Parse("[\"heat\",\"cool\",\"off\"]").RootElement.Clone();
        var e = Entity("climate.x", new() { ["hvac_modes"] = (object)json });
        e.GetHvacModes().ShouldBe(["heat", "cool", "off"]);
    }

    [Fact]
    public void GetHvacModes_Missing_ReturnsEmptyList()
    {
        Entity("climate.x").GetHvacModes().ShouldBeEmpty();
    }
}
