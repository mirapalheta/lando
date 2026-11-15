using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.CustomSkill.Models;
using Lando.Alexa.CustomSkill.Services;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging;

namespace Lando.Alexa.CustomSkill.Handlers;

/// <summary>
/// Top-level handler for Alexa Custom Skill requests. Routes an
/// <c>IntentRequest</c> to the HA script bound to that intent (via
/// <see cref="IIntentScriptResolver"/>), maps the intent's slots onto the
/// script's fields, and runs it. <c>LaunchRequest</c> and the built-in
/// AMAZON.* intents get spoken acknowledgements without touching HA.
/// </summary>
/// <remarks>
/// A script without <c>alexa_response</c> runs fire-and-forget
/// (<c>script.turn_on</c>) and gets "Okay, running X.". A script with
/// <c>alexa_response: true</c> runs blocking with <c>return_response</c> under
/// <see cref="ResponseTimeout"/>; a non-empty string <c>speech</c> in its
/// response is spoken as is, anything else falls back to "Okay, running X.".
/// </remarks>
/// <remarks>
/// This is the intent-path analogue of <c>SmartHomeHandler</c> — same
/// <see cref="IRequestHandler{TRequest,TResponse}"/> seam, different wire
/// format. It reuses the HMAC-validated transport (registered by
/// <c>AddRequestHandler</c>) and the shared <see cref="IHomeAssistantClient"/>.
/// </remarks>
public sealed class IntentSkillHandler(
    IHomeAssistantClient client,
    IIntentScriptResolver resolver,
    ILogger<IntentSkillHandler> logger) : IRequestHandler<IntentRequest, IntentResponse>
{
    private const string RequestTypeIntent = "IntentRequest";
    private const string RequestTypeLaunch = "LaunchRequest";
    private const string ResolutionMatch = "ER_SUCCESS_MATCH";
    private const string SpeechKey = "speech";
    private const string SlowAnswer = "That is taking longer than usual. Check Home Assistant.";

    /// <summary>
    /// How long a flagged script may take before lando answers without it. The
    /// Alexa proxy Lambda gives up at 8 s, so this leaves room for the round trip.
    /// HA keeps running the script after lando stops waiting.
    /// </summary>
    internal TimeSpan ResponseTimeout { get; init; } = TimeSpan.FromSeconds(6);

    /// <inheritdoc />
    public async Task<IntentResponse> HandleAsync(IntentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var type = request.Request.Type;

            if (type == RequestTypeLaunch)
                return IntentResponse.Speak("Lando is ready. Try asking me to run one of your routines.", endSession: false);

            if (type != RequestTypeIntent)
                return new IntentResponse(); // SessionEndedRequest and friends — nothing to say.

            var intent = request.Request.Intent
                ?? throw new InvalidOperationException("IntentRequest is missing its intent");

            if (IsBuiltInIntent(intent.Name))
                return HandleBuiltIn(intent.Name);

            logger.LogInformation("Custom skill intent received: {Intent}", intent.Name);

            var script = await resolver.ResolveAsync(intent.Name, cancellationToken).ConfigureAwait(false);
            if (script is null)
            {
                logger.LogWarning("No script is bound to intent {Intent}", intent.Name);
                return IntentResponse.Speak("Sorry, I don't have anything set up for that.");
            }

            var variables = BuildVariables(intent.Slots, script.SlotMap);
            if (script.WantsResponse)
                return await RunWithResponseAsync(intent.Name, script, variables, cancellationToken).ConfigureAwait(false);

            await client.CallServiceAsync(HomeAssistantRequest.RunScript(script.EntityId, variables), cancellationToken).ConfigureAwait(false);

            return IntentResponse.Speak(Confirmation(script));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error handling custom skill request");
            return IntentResponse.Speak("Sorry, something went wrong.");
        }
    }

    /// <summary>
    /// Runs a flagged script with <c>return_response</c> under
    /// <see cref="ResponseTimeout"/> and speaks its <c>speech</c>.
    /// </summary>
    private async Task<IntentResponse> RunWithResponseAsync(
        string intentName, IntentScript script, Dictionary<string, object?> variables, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ResponseTimeout);

        JsonElement? response;
        try
        {
            response = await client.RunScriptWithResponseAsync(script.EntityId, variables, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Intent {Intent}: {EntityId} did not answer within {TimeoutMs}ms", intentName, script.EntityId, (int)ResponseTimeout.TotalMilliseconds);
            return IntentResponse.Speak(SlowAnswer);
        }

        var speech = ReadSpeech(response);
        logger.LogInformation("Intent {Intent}: {EntityId} answered, speech returned: {HasSpeech}", intentName, script.EntityId, speech is not null);

        return IntentResponse.Speak(speech ?? Confirmation(script));
    }

    /// <summary>
    /// The response's <c>speech</c> when it is a non-blank string, else null.
    /// </summary>
    private static string? ReadSpeech(JsonElement? response)
        => response is { ValueKind: JsonValueKind.Object } element
           && element.TryGetProperty(SpeechKey, out var speech)
           && speech.ValueKind == JsonValueKind.String
           && !string.IsNullOrWhiteSpace(speech.GetString())
            ? speech.GetString()
            : null;

    private static string Confirmation(IntentScript script) => $"Okay, running {script.FriendlyName ?? "that"}.";

    /// <summary>
    /// Maps each configured Alexa slot to its script field, skipping empty slots.
    /// </summary>
    private static Dictionary<string, object?> BuildVariables(
        IReadOnlyDictionary<string, Slot>? slots, IReadOnlyDictionary<string, string> slotMap)
    {
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (slots is null)
            return variables;

        foreach (var (alexaSlot, field) in slotMap)
        {
            if (slots.TryGetValue(alexaSlot, out var slot) && ResolveSlotValue(slot) is { } value)
                variables[field] = value;
        }

        return variables;
    }

    /// <summary>
    /// Resolve the value to forward to the script. Prefer a custom slot value's
    /// <c>id</c> (set to the target identifier — e.g. a routine's HA script
    /// object_id), then its canonical <c>name</c>, then the raw spoken value.
    /// </summary>
    /// <remarks>
    /// Built-in slots like AMAZON.TIME have no resolutions and arrive already
    /// normalized in <see cref="Slot.Value"/>.
    /// </remarks>
    private static string? ResolveSlotValue(Slot slot)
    {
        var resolved = slot.Resolutions?.PerAuthority?
            .FirstOrDefault(authority => authority.Status?.Code == ResolutionMatch)?
            .Values?.FirstOrDefault()?.Value;

        var canonical = string.IsNullOrWhiteSpace(resolved?.Id) ? resolved?.Name : resolved.Id;

        return string.IsNullOrWhiteSpace(canonical) ? slot.Value : canonical;
    }

    private static bool IsBuiltInIntent(string name) => name.StartsWith("AMAZON.", StringComparison.Ordinal);

    private static IntentResponse HandleBuiltIn(string name) => name switch
    {
        "AMAZON.HelpIntent" => IntentResponse.Speak("You can ask me to run one of your Home Assistant routines.", endSession: false),
        "AMAZON.StopIntent" or "AMAZON.CancelIntent" => IntentResponse.Speak("Okay."),
        _ => IntentResponse.Speak("Sorry, I didn't catch that.")
    };
}
