using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Lando.Alexa.CustomSkill.Services;

/// <summary>
/// Builds (and briefly caches) the intent→script map by scanning exposed HA
/// <c>script.*</c> entities for an <c>alexa_intent</c> attribute. The cache
/// avoids hitting HA on every utterance; the TTL is short so a newly-flagged
/// script is picked up without a restart.
/// </summary>
internal sealed class IntentScriptResolver(IHomeAssistantClient client, IMemoryCache cache) : IIntentScriptResolver
{
    private const string CacheKey = "lando:custom-skill:intent-map";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    public async Task<IntentScript?> ResolveAsync(string intentName, CancellationToken cancellationToken)
    {
        var map = await GetMapAsync(cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(intentName, out var script) ? script : null;
    }

    private async Task<IReadOnlyDictionary<string, IntentScript>> GetMapAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, IntentScript>? cached) && cached is not null)
            return cached;

        var map = new Dictionary<string, IntentScript>(StringComparer.OrdinalIgnoreCase);
        await foreach (var entity in client.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            if (entity.GetDomain() != Lando.HomeAssistant.Constants.Domains.Script)
                continue;

            var intent = entity.Attributes.GetString(Constants.CustomAttributes.Intent);
            if (string.IsNullOrWhiteSpace(intent))
                continue;

            // Last write wins if two scripts claim the same intent; that's a
            // misconfiguration, but deterministic enough not to need a throw.
            map[intent] = new IntentScript(entity.EntityId, entity.GetFriendlyName(), ReadSlotMap(entity));
        }

        cache.Set(CacheKey, (IReadOnlyDictionary<string, IntentScript>)map, Ttl);
        return map;
    }

    /// <summary>
    /// Reads the <c>alexa_slots</c> object attribute into a string→string map.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ReadSlotMap(HomeAssistantEntity entity)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (entity.Attributes is null || !entity.Attributes.TryGetValue(Constants.CustomAttributes.Slots, out var raw))
            return result;

        if (raw is JsonElement { ValueKind: JsonValueKind.Object } element)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString()!;
            }
        }

        return result;
    }
}
