using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Configuration;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lando.HomeAssistant.Services;

/// <summary>
/// Connects to the Home Assistant WebSocket API, authenticates with the long-lived token,
/// subscribes to <c>state_changed</c> events, and yields them as an async stream.
/// </summary>
/// <remarks>
/// Certificate and proxy settings are taken from <see cref="HomeAssistantConfiguration"/>
/// so the WebSocket uses the same trust anchors and routing as the REST HTTP client.
/// Protocol-level failures (handshake, auth, subscribe) surface as
/// <see cref="HomeAssistantException"/>. Per-event parse errors are logged and skipped
/// rather than tearing down the subscription.
/// </remarks>
public sealed class HomeAssistantWebSocketClient(
    [FromKeyedServices(Constants.HomeAssistant)] SocketsHttpHandler httpHandler,
    HomeAssistantClientOptions options,
    ILogger<HomeAssistantWebSocketClient> logger) : IHomeAssistantWebSocketClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async IAsyncEnumerable<HomeAssistantStateChangedEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var uri = options.WebSocketUri();
        using var ws = new ClientWebSocket();
        using var invoker = new HttpMessageInvoker(httpHandler, disposeHandler: false);

        await ConnectAndSubscribeAsync(ws, uri, invoker, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("HA WebSocket subscription established; waiting for state_changed events");

        await foreach (var ev in ReadEventsAsync(ws, cancellationToken).ConfigureAwait(false))
            yield return ev;
    }

    private async Task ConnectAndSubscribeAsync(
        ClientWebSocket ws,
        Uri uri,
        HttpMessageInvoker invoker,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Connecting to HA WebSocket at {Uri}", uri);
            await ws.ConnectAsync(uri, invoker, cancellationToken).ConfigureAwait(false);

            await AuthenticateAsync(ws, cancellationToken).ConfigureAwait(false);
            await SubscribeStateChangedAsync(ws, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HomeAssistantException and not OperationCanceledException)
        {
            logger.LogError(ex, "HA WebSocket connect/subscribe failed for {Uri}", uri);
            throw new HomeAssistantException($"Home Assistant WebSocket connect/subscribe failed: {uri}", ex);
        }
    }

    private async Task AuthenticateAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var authRequired = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
        if (authRequired?["type"]?.GetValue<string>() != "auth_required")
            throw new HomeAssistantException($"Expected auth_required from HA WebSocket, got: {authRequired}");

        var authPayload = JsonSerializer.Serialize(new
        {
            type = "auth",
            access_token = options.Token
        });
        await SendMessageAsync(ws, authPayload, ct).ConfigureAwait(false);

        var authResult = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
        var resultType = authResult?["type"]?.GetValue<string>();
        if (resultType == "auth_invalid")
            throw new HomeAssistantException("HA WebSocket authentication failed: auth_invalid");
        if (resultType != "auth_ok")
            throw new HomeAssistantException($"Unexpected HA WebSocket auth response: {authResult}");
    }

    private async Task SubscribeStateChangedAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var subscribePayload = JsonSerializer.Serialize(new
        {
            id = 1,
            type = "subscribe_events",
            event_type = "state_changed"
        });
        await SendMessageAsync(ws, subscribePayload, ct).ConfigureAwait(false);

        var result = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
        if (result?["success"]?.GetValue<bool>() != true)
            throw new HomeAssistantException($"HA WebSocket subscribe_events failed: {result}");
    }

    private async IAsyncEnumerable<HomeAssistantStateChangedEvent> ReadEventsAsync(
        ClientWebSocket ws,
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (ws.State == WebSocketState.Open)
        {
            ct.ThrowIfCancellationRequested();
            var next = default(HomeAssistantStateChangedEvent?);
            try
            {
                var message = await ReceiveMessageAsync(ws, ct).ConfigureAwait(false);
                if (message is null)
                    break;

                if (message["type"]?.GetValue<string>() != "event")
                    continue;

                var data = message["event"]?["data"];
                if (data is null)
                    continue;

                next = ParseStateChangedEvent(data);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single malformed message shouldn't tear down the subscription — log
                // and continue. Transport-fatal failures are surfaced via ws.State on the
                // next loop iteration.
                logger.LogWarning(ex, "Skipping malformed HA WebSocket event");
                continue;
            }

            if (next is not null)
                yield return next;
        }

        ct.ThrowIfCancellationRequested();
    }

    private static HomeAssistantStateChangedEvent? ParseStateChangedEvent(JsonNode data)
    {
        var entityId = data["entity_id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(entityId))
            return null;

        return new HomeAssistantStateChangedEvent
        {
            EntityId = entityId,
            NewState = data["new_state"]?.Deserialize<HomeAssistantEntity>(JsonOptions),
            OldState = data["old_state"]?.Deserialize<HomeAssistantEntity>(JsonOptions)
        };
    }

    private static async Task SendMessageAsync(ClientWebSocket ws, string payload, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
    }

    private static async Task<JsonNode?> ReceiveMessageAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new ArraySegment<byte>(new byte[64 * 1024]);
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        }
        while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);
        return JsonNode.Parse(ms);
    }
}
