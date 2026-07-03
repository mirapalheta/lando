using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.CustomSkill.Models;
using Lando.Alexa.CustomSkill.Services;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.Alexa.CustomSkill.Handlers.Tests;

/// <summary>
/// <see cref="IntentSkillHandler"/> routes Alexa custom-skill intents to the
/// bound HA script, maps slots onto script fields, and answers with spoken
/// confirmations. These pin happy-path dispatch + slot mapping, canonical slot
/// resolution, the unknown-intent and built-in paths, and that non-intent
/// request types don't touch HA.
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
}
