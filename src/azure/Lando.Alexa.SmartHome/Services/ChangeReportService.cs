using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lando.Alexa.SmartHome.Services;

using static Lando.Alexa.SmartHome.Constants;

/// <summary>
/// Long-running hosted service that subscribes to Home Assistant WebSocket
/// <c>state_changed</c> events and forwards them as proactive
/// <c>Alexa.ChangeReport</c> events to the Alexa Event Gateway for every
/// registered grantee.
/// </summary>
/// <remarks>
/// <para>
/// The service reconnects automatically with exponential back-off when the HA
/// WebSocket connection drops — expected behaviour when HA restarts or the
/// Tailscale tunnel hiccups.
/// </para>
/// <para>
/// Only entities that carry the <c>alexa_expose: true</c> custom attribute are
/// forwarded; all others are silently ignored.
/// </para>
/// </remarks>
internal sealed class ChangeReportService(
    IHomeAssistantWebSocketClient wsClient,
    IEventGatewayClient gatewayClient,
    IEntityTransform<ContextProperty[]> transformer,
    ILogger<ChangeReportService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ChangeReportService starting");

        var delay = InitialReconnectDelay;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken).ConfigureAwait(false);
                // RunAsync returning without throwing means the upstream enumerable
                // ended cleanly (HA closed the WS politely). Treat it like a transient
                // failure: wait, back off, reconnect — otherwise we'd hot-loop.
                logger.LogWarning("HA WebSocket subscription ended without error; reconnecting in {Delay}", delay);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HA WebSocket connection lost; reconnecting in {Delay}", delay);
            }

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            delay = delay < MaxReconnectDelay
                ? TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds))
                : MaxReconnectDelay;
        }

        logger.LogInformation("ChangeReportService stopped");
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await foreach (var ev in wsClient.SubscribeAsync(ct).ConfigureAwait(false))
        {
            // We only forward changes on currently-exposed entities. If the entity used
            // to be exposed and now isn't (or vice versa), we leave that to a future
            // AddOrUpdateReport / DeleteReport publisher — that's a different event.
            if (ev.NewState?.IsExposed(CustomAttributes.Expose) != true)
                continue;

            var newProperties = transformer.Transform(ev.NewState);
            if (newProperties is null || newProperties.Length == 0)
                continue;

            // Diff against the previous snapshot when we have one. First-seen entities
            // (OldState == null) and entities transitioning from un-exposed → exposed
            // are reported as "everything changed."
            var oldProperties = ev.OldState?.IsExposed(CustomAttributes.Expose) == true
                ? transformer.Transform(ev.OldState)
                : null;

            var changed = ComputeDelta(oldProperties, newProperties);
            if (changed.Length == 0)
            {
                // The state_changed event fired but no Alexa-visible property actually
                // moved (e.g. only last_updated timestamp). Skip — sending an empty
                // ChangeReport would be both noisy and rejected by the gateway.
                continue;
            }

            logger.LogInformation(
                "Entity '{EntityId}' has {ChangeCount} changed property(ies): {ChangedProperties}",
                ev.EntityId, changed.Length, string.Join(";", changed.Select(p => $"{p.Name}={p.Value}")));

            // Use the Alexa-formatted endpoint id (e.g. "switch#back_bedroom_lights"),
            // NOT the raw HA entity_id ("switch.back_bedroom_lights"). The endpoint set
            // Alexa stored at Discovery uses '#' as the separator; sending a ChangeReport
            // keyed on the dotted form is silently accepted (gateway returns 202) but
            // never routes to a known endpoint, so the customer's UI never updates.
            await gatewayClient
                .SendChangeReportAsync(ev.EndpointId(), changed, newProperties, ct)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns the subset of <paramref name="new"/> whose <c>(Namespace, Instance, Name)</c>
    /// either didn't exist in <paramref name="old"/> or had a different <see cref="ContextProperty.Value"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <paramref name="old"/> is null/empty (first-seen entity), the entire new set
    /// is treated as changed — Alexa accepts this as the initial proactive state.
    /// </para>
    /// <para>
    /// Value equality first tries <see cref="object.Equals(object?)"/>, which handles
    /// primitives, strings, and enum strings cheaply. For composite values like
    /// <c>Temperature</c> or <c>HsbColor</c> we fall back to JSON shape comparison so the
    /// diff is correct without requiring every payload type to override Equals.
    /// </para>
    /// </remarks>
    private static ContextProperty[] ComputeDelta(ContextProperty[]? old, ContextProperty[] @new)
    {
        if (old is null || old.Length == 0)
            return @new;

        var oldLookup = new Dictionary<string, ContextProperty>(old.Length);
        foreach (var p in old)
            oldLookup[Key(p)] = p;

        var delta = new List<ContextProperty>(@new.Length);
        foreach (var n in @new)
        {
            if (!oldLookup.TryGetValue(Key(n), out var o) || !ValuesEqual(o.Value, n.Value))
                delta.Add(n);
        }

        return delta.ToArray();
    }

    private static string Key(ContextProperty p)
        => $"{p.Namespace}|{p.Instance}|{p.Name}";

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        if (a.Equals(b))
            return true;
        // Composite shapes (Temperature, HsbColor, etc.): fall back to JSON equality.
        // This is O(serialize), but state_changed events are low-frequency and these
        // payloads are tiny.
        return JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
    }
}
