using System.Collections.Generic;

namespace Lando.Alexa.CustomSkill.Services;

/// <summary>
/// A script that has opted into voice-intent routing, with its slot→field map.
/// </summary>
/// <param name="EntityId">The HA script entity id, e.g. <c>script.example_routine</c>.</param>
/// <param name="FriendlyName">Spoken name for confirmations.</param>
/// <param name="SlotMap">Alexa slot name → script field name.</param>
/// <param name="WantsResponse">
/// The script set <c>alexa_response: true</c>: run it blocking with
/// <c>return_response</c> and speak its <c>speech</c>.
/// </param>
public sealed record IntentScript(string EntityId, string? FriendlyName, IReadOnlyDictionary<string, string> SlotMap, bool WantsResponse = false);
