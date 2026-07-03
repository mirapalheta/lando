using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Lando.Alexa.CustomSkill.Services.Tests;

/// <summary>
/// <see cref="IntentScriptResolver"/> scans exposed HA <c>script.*</c> entities
/// for an <c>alexa_intent</c> attribute and builds (and briefly caches) an
/// intent-name -> script map. These pin domain filtering, missing/blank intent
/// handling, slot-map parsing (including malformed shapes), case-insensitive
/// lookup, last-write-wins on a duplicate intent, and that the map is cached
/// across calls rather than re-listing HA entities every time.
/// </summary>
public class IntentScriptResolverTests
{
    [Fact]
    public async Task Resolves_script_bound_to_intent()
    {
        var entity = ScriptEntity("script.example_routine", "RunRoutine", "Example Routine",
            slots: """{"level":"level","duration":"duration"}""");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldNotBeNull();
        script.EntityId.ShouldBe("script.example_routine");
        script.FriendlyName.ShouldBe("Example Routine");
        script.SlotMap["level"].ShouldBe("level");
        script.SlotMap["duration"].ShouldBe("duration");
    }

    [Fact]
    public async Task Returns_null_when_no_script_matches()
    {
        var entity = ScriptEntity("script.other", "SomethingElse", "Other");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldBeNull();
    }

    [Fact]
    public async Task Ignores_non_script_domains_even_with_an_intent_attribute()
    {
        // A light shouldn't be intent-routable even if it happens to carry the
        // same custom attribute -- only the `script` domain is scanned.
        var entity = new HomeAssistantEntity
        {
            EntityId = "light.living_room",
            Attributes = new Dictionary<string, object> { ["alexa_intent"] = "RunRoutine" },
        };
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Skips_scripts_with_a_blank_intent_attribute(string intent)
    {
        var entity = new HomeAssistantEntity
        {
            EntityId = "script.no_intent",
            Attributes = new Dictionary<string, object> { ["alexa_intent"] = intent },
        };
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldBeNull();
    }

    [Fact]
    public async Task Skips_scripts_with_no_intent_attribute_at_all()
    {
        var entity = new HomeAssistantEntity { EntityId = "script.no_intent", Attributes = [] };
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldBeNull();
    }

    [Fact]
    public async Task Missing_slots_attribute_yields_empty_slot_map()
    {
        var entity = ScriptEntity("script.no_slots", "RunRoutine", "No Slots");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldNotBeNull();
        script.SlotMap.ShouldBeEmpty();
    }

    [Fact]
    public async Task Non_object_slots_attribute_yields_empty_slot_map()
    {
        // alexa_slots is documented as an object map; a malformed value (e.g. a
        // bare string) should degrade to no slots rather than throw.
        var entity = ScriptEntity("script.bad_slots", "RunRoutine", "Bad Slots", slots: "\"not-an-object\"");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script.ShouldNotBeNull();
        script.SlotMap.ShouldBeEmpty();
    }

    [Fact]
    public async Task Non_string_slot_values_are_skipped()
    {
        var entity = ScriptEntity("script.mixed_slots", "RunRoutine", "Mixed",
            slots: """{"level":"level","count":5}""");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script!.SlotMap.ShouldContainKey("level");
        script.SlotMap.ShouldNotContainKey("count");
    }

    [Fact]
    public async Task Intent_lookup_is_case_insensitive()
    {
        var entity = ScriptEntity("script.example_routine", "RunRoutine", "Example Routine");
        var sut = Sut([entity]);

        var script = await sut.ResolveAsync("runroutine", CancellationToken.None);

        script.ShouldNotBeNull();
    }

    [Fact]
    public async Task Last_script_wins_when_two_claim_the_same_intent()
    {
        var first = ScriptEntity("script.first", "RunRoutine", "First");
        var second = ScriptEntity("script.second", "RunRoutine", "Second");
        var sut = Sut([first, second]);

        var script = await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        script!.EntityId.ShouldBe("script.second");
    }

    [Fact]
    public async Task Caches_the_map_so_a_second_call_does_not_relist_entities()
    {
        var entity = ScriptEntity("script.example_routine", "RunRoutine", "Example Routine");
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.ListAsync(It.IsAny<CancellationToken>())).Returns(ToAsync([entity]));
        var sut = new IntentScriptResolver(client.Object, new MemoryCache(new MemoryCacheOptions()));

        await sut.ResolveAsync("RunRoutine", CancellationToken.None);
        await sut.ResolveAsync("RunRoutine", CancellationToken.None);

        client.Verify(c => c.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IntentScriptResolver Sut(IEnumerable<HomeAssistantEntity> entities)
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.ListAsync(It.IsAny<CancellationToken>())).Returns(ToAsync(entities));
        return new IntentScriptResolver(client.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    private static HomeAssistantEntity ScriptEntity(string entityId, string intent, string friendlyName, string? slots = null)
    {
        var attrs = new Dictionary<string, object>
        {
            ["alexa_intent"] = intent,
            ["friendly_name"] = friendlyName,
        };
        if (slots is not null)
            attrs["alexa_slots"] = JsonDocument.Parse(slots).RootElement;

        return new HomeAssistantEntity { EntityId = entityId, Attributes = attrs };
    }

    private static async IAsyncEnumerable<HomeAssistantEntity> ToAsync(
        IEnumerable<HomeAssistantEntity> entities,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var e in entities)
        { ct.ThrowIfCancellationRequested(); yield return e; await Task.Yield(); }
    }
}
