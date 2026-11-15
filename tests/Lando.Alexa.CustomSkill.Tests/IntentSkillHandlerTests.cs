using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.CustomSkill.Models;
using Lando.Alexa.CustomSkill.Services;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.Alexa.CustomSkill.Handlers.Tests;

/// <summary>
/// <see cref="IntentSkillHandler"/> routes Alexa custom-skill intents to the
/// bound HA script, maps slots onto script fields, and answers with spoken
/// confirmations. These pin happy-path dispatch + slot mapping, canonical slot
/// resolution, the unknown-intent and built-in paths, that non-intent
/// request types don't touch HA, and the <c>alexa_response</c> path: a flagged
/// script runs with <c>return_response</c> and its <c>speech</c> is spoken.
/// </summary>
public class IntentSkillHandlerTests
{
    private static IntentRequest IntentReq(string intentName, Dictionary<string, Slot>? slots = null) => new()
    {
        Request = new RequestEnvelope { Type = "IntentRequest", Intent = new Intent { Name = intentName, Slots = slots } }
    };

    private static IntentSkillHandler Sut(IHomeAssistantClient client, IIntentScriptResolver resolver)
        => new(client, resolver, NullLogger<IntentSkillHandler>.Instance);

    [Fact]
    public async Task Routes_intent_to_script_and_runs_it_with_mapped_slots()
    {
        var resolver = new Mock<IIntentScriptResolver>();
        resolver.Setup(r => r.ResolveAsync("RunRoutine", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentScript("script.example_routine", "Example Routine",
                new Dictionary<string, string> { ["level"] = "level", ["duration"] = "duration" }));
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, resolver.Object);

        var slots = new Dictionary<string, Slot>
        {
            ["level"] = new Slot { Name = "level", Value = "40" },
            ["duration"] = new Slot { Name = "duration", Value = "PT10M" },
        };

        var response = await sut.HandleAsync(IntentReq("RunRoutine", slots), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldContain("Example Routine");
        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == "script.example_routine" &&
            r.Service == "turn_on" &&
            r.Variables != null &&
            (string)r.Variables!["level"]! == "40" &&
            (string)r.Variables!["duration"]! == "PT10M"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prefers_canonical_resolution_over_spoken_value()
    {
        var resolver = new Mock<IIntentScriptResolver>();
        resolver.Setup(r => r.ResolveAsync("SetPreset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentScript("script.example_routine", "Example Routine",
                new Dictionary<string, string> { ["preset"] = "preset" }));
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, resolver.Object);

        var slot = new Slot
        {
            Name = "preset",
            Value = "movie",
            Resolutions = new Resolutions
            {
                PerAuthority =
                [
                    new ResolutionAuthority
                    {
                        Status = new ResolutionStatus { Code = "ER_SUCCESS_MATCH" },
                        Values = [new ResolutionValueWrapper { Value = new ResolutionValue { Name = "movie_mode" } }]
                    }
                ]
            }
        };

        await sut.HandleAsync(IntentReq("SetPreset", new() { ["preset"] = slot }), CancellationToken.None);

        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            (string)r.Variables!["preset"]! == "movie_mode"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prefers_resolution_id_over_canonical_name()
    {
        // A custom slot value's id carries the target identifier (e.g. the HA
        // script object_id); it must win over the spoken-friendly canonical name.
        var resolver = new Mock<IIntentScriptResolver>();
        resolver.Setup(r => r.ResolveAsync("ScheduleAdd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentScript("script.lando_schedule_add", "Lando - Schedule - Add",
                new Dictionary<string, string> { ["routine"] = "routine" }));
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, resolver.Object);

        var slot = new Slot
        {
            Name = "routine",
            Value = "wake up master bedroom",
            Resolutions = new Resolutions
            {
                PerAuthority =
                [
                    new ResolutionAuthority
                    {
                        Status = new ResolutionStatus { Code = "ER_SUCCESS_MATCH" },
                        Values =
                        [
                            new ResolutionValueWrapper
                            {
                                Value = new ResolutionValue { Name = "wake up master bedroom", Id = "shades_master_bedroom_wake_up" }
                            }
                        ]
                    }
                ]
            }
        };

        await sut.HandleAsync(IntentReq("ScheduleAdd", new() { ["routine"] = slot }), CancellationToken.None);

        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            (string)r.Variables!["routine"]! == "shades_master_bedroom_wake_up"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unknown_intent_speaks_and_does_not_call_HA()
    {
        var resolver = new Mock<IIntentScriptResolver>();
        resolver.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IntentScript?)null);
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("DoesNotExist"), CancellationToken.None);

        response.Response.ShouldEndSession.ShouldBeTrue();
        client.Verify(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LaunchRequest_keeps_session_open()
    {
        var sut = Sut(new Mock<IHomeAssistantClient>().Object, new Mock<IIntentScriptResolver>().Object);

        var response = await sut.HandleAsync(
            new IntentRequest { Request = new RequestEnvelope { Type = "LaunchRequest" } }, CancellationToken.None);

        response.Response.ShouldEndSession.ShouldBeFalse();
    }

    [Fact]
    public async Task Builtin_stop_intent_is_acknowledged_without_calling_HA()
    {
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, new Mock<IIntentScriptResolver>().Object);

        await sut.HandleAsync(IntentReq("AMAZON.StopIntent"), CancellationToken.None);

        client.Verify(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IntentScript FlaggedModeScript() => new("script.lando_set_home_mode", "Lando - Set Home Mode",
        new Dictionary<string, string> { ["mode"] = "mode" }, WantsResponse: true);

    private static Dictionary<string, Slot> ModeSlot(string value) => new() { ["mode"] = new Slot { Name = "mode", Value = value } };

    private static Mock<IIntentScriptResolver> ResolverFor(string intent, IntentScript script)
    {
        var resolver = new Mock<IIntentScriptResolver>();
        resolver.Setup(r => r.ResolveAsync(intent, It.IsAny<CancellationToken>())).ReturnsAsync(script);
        return resolver;
    }

    private static Mock<IHomeAssistantClient> ClientAnswering(string json)
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonDocument.Parse(json).RootElement.Clone());
        return client;
    }

    [Fact]
    public async Task Flagged_script_speaks_the_returned_speech_and_never_calls_turn_on()
    {
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = ClientAnswering("""{"speech":"I can't switch to home mode by voice. Use the keypad or the app.","allowed":false}""");
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("home")), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("I can't switch to home mode by voice. Use the keypad or the app.");
        response.Response.ShouldEndSession.ShouldBeTrue();
        client.Verify(c => c.RunScriptWithResponseAsync("script.lando_set_home_mode",
            It.Is<IReadOnlyDictionary<string, object?>?>(v => v != null && (string)v["mode"]! == "home"),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"speech":42}""")]
    [InlineData("""{"speech":""}""")]
    [InlineData("""{"speech":"   "}""")]
    [InlineData("""{"speech":null}""")]
    [InlineData("""["speech"]""")]
    public async Task Flagged_script_without_a_usable_speech_falls_back_to_the_confirmation(string json)
    {
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = ClientAnswering(json);
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("sleep")), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("Okay, running Lando - Set Home Mode.");
        client.Verify(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Flagged_script_with_no_service_response_falls_back_to_the_confirmation()
    {
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonElement?)null);
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("sleep")), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("Okay, running Lando - Set Home Mode.");
    }

    [Fact]
    public async Task Unflagged_script_keeps_turn_on_and_never_waits_for_a_response()
    {
        var resolver = ResolverFor("ScheduleList", new IntentScript("script.lando_schedule_list", "Lando - Schedule - List",
            new Dictionary<string, string>()));
        var client = new Mock<IHomeAssistantClient>();
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("ScheduleList"), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("Okay, running Lando - Schedule - List.");
        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == "script.lando_schedule_list" && r.Service == "turn_on"), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Flagged_script_that_outlives_the_timeout_speaks_the_slow_sentence()
    {
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .Returns<string, IReadOnlyDictionary<string, object?>?, CancellationToken>(async (_, _, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        var sut = new IntentSkillHandler(client.Object, resolver.Object, NullLogger<IntentSkillHandler>.Instance)
        {
            ResponseTimeout = TimeSpan.FromMilliseconds(50),
        };

        var response = await sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("away")), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("That is taking longer than usual. Check Home Assistant.");
    }

    [Fact]
    public async Task Flagged_script_cancelled_by_the_caller_is_not_reported_as_slow()
    {
        // The request's own token (Alexa or the host gave up) still propagates,
        // as it does on the fire-and-forget path.
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .Returns<string, IReadOnlyDictionary<string, object?>?, CancellationToken>(async (_, _, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        var sut = Sut(client.Object, resolver.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("away")), cts.Token));
    }

    [Fact]
    public async Task Flagged_script_HA_failure_still_says_something_went_wrong()
    {
        var resolver = ResolverFor("SetHomeMode", FlaggedModeScript());
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.RunScriptWithResponseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HomeAssistantException("Error running script script.lando_set_home_mode: BadGateway"));
        var sut = Sut(client.Object, resolver.Object);

        var response = await sut.HandleAsync(IntentReq("SetHomeMode", ModeSlot("home")), CancellationToken.None);

        response.Response.OutputSpeech!.Text.ShouldBe("Sorry, something went wrong.");
    }
}
