using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Configuration;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Lando.HomeAssistant.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.HomeAssistant.Core.Services.Tests;

/// <summary>
/// Uses an in-process <see cref="HttpListener"/>-backed WebSocket server to
/// drive <see cref="HomeAssistantWebSocketClient"/> through its full protocol:
/// connect → auth handshake → subscribe → event stream.
/// </summary>
public class HomeAssistantWebSocketClientTests
{
    // ── Connection failure ────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_ConnectionRefused_ThrowsHomeAssistantException()
    {
        // Port 1 is reserved and will be refused on all platforms.
        var sut = BuildSut(port: 1);

        var ex = await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in sut.SubscribeAsync())
            { }
        });

        ex.InnerException.ShouldNotBeNull();
    }

    // ── Auth handshake failures ───────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_AuthRequired_NotReceived_ThrowsHomeAssistantException()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await SendJson(ws, new { type = "unexpected" }, ct);
            await CloseAsync(ws);
        });

        var ex = await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in BuildSut(server.Port).SubscribeAsync())
            { }
        });

        ex.Message.ShouldContain("auth_required");
    }

    [Fact]
    public async Task SubscribeAsync_AuthInvalid_ThrowsHomeAssistantException()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await SendJson(ws, new { type = "auth_required" }, ct);
            await ReceiveJson(ws, ct);
            await SendJson(ws, new { type = "auth_invalid" }, ct);
            await CloseAsync(ws);
        });

        var ex = await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in BuildSut(server.Port).SubscribeAsync())
            { }
        });

        ex.Message.ShouldContain("auth_invalid");
    }

    [Fact]
    public async Task SubscribeAsync_UnexpectedAuthResponse_ThrowsHomeAssistantException()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await SendJson(ws, new { type = "auth_required" }, ct);
            await ReceiveJson(ws, ct);
            await SendJson(ws, new { type = "something_else" }, ct);
            await CloseAsync(ws);
        });

        var ex = await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in BuildSut(server.Port).SubscribeAsync())
            { }
        });

        ex.Message.ShouldContain("Unexpected");
    }

    // ── Subscribe failure ─────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_SubscribeResultNotSuccess_ThrowsHomeAssistantException()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await SendJson(ws, new { type = "auth_required" }, ct);
            await ReceiveJson(ws, ct);
            await SendJson(ws, new { type = "auth_ok" }, ct);
            await ReceiveJson(ws, ct);
            await SendJson(ws, new { success = false, error = new { code = "unknown_error" } }, ct);
            await CloseAsync(ws);
        });

        var ex = await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in BuildSut(server.Port).SubscribeAsync())
            { }
        });

        ex.Message.ShouldContain("subscribe_events");
    }

    // ── Event stream ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_SingleEvent_IsYielded()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            await SendStateChangedEvent(ws, "light.kitchen", newState: "on", oldState: "off", ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.ShouldHaveSingleItem();
        events[0].EntityId.ShouldBe("light.kitchen");
        events[0].NewState!.State.ShouldBe("on");
        events[0].OldState!.State.ShouldBe("off");
    }

    [Fact]
    public async Task SubscribeAsync_MultipleEvents_AreYieldedInOrder()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            await SendStateChangedEvent(ws, "light.one", newState: "on", oldState: "off", ct);
            await SendStateChangedEvent(ws, "switch.two", newState: "off", oldState: "on", ct);
            await SendStateChangedEvent(ws, "cover.three", newState: "open", oldState: "closed", ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.Count.ShouldBe(3);
        events.Select(e => e.EntityId).ShouldBe(["light.one", "switch.two", "cover.three"]);
    }

    [Fact]
    public async Task SubscribeAsync_NonEventTypeMessage_IsSkipped()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            await SendJson(ws, new { type = "pong" }, ct);           // not "event"
            await SendStateChangedEvent(ws, "light.kitchen", "on", "off", ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.ShouldHaveSingleItem();
        events[0].EntityId.ShouldBe("light.kitchen");
    }

    [Fact]
    public async Task SubscribeAsync_EventMissingEntityId_IsSkipped()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            // event without entity_id in data
            await SendJson(ws, new
            {
                type = "event",
                @event = new { data = new { new_state = new { state = "on" } } }
            }, ct);
            await SendStateChangedEvent(ws, "light.valid", "on", "off", ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.ShouldHaveSingleItem();
        events[0].EntityId.ShouldBe("light.valid");
    }

    [Fact]
    public async Task SubscribeAsync_MalformedJsonEvent_IsSkippedAndStreamContinues()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            // raw invalid JSON — triggers JsonException in ReceiveMessageAsync,
            // caught by the per-event handler, loop continues
            var garbage = Encoding.UTF8.GetBytes("{ not valid json !!!");
            await ws.SendAsync(garbage.AsMemory(), WebSocketMessageType.Text, true, ct);
            await SendStateChangedEvent(ws, "light.after", "on", "off", ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.ShouldHaveSingleItem();
        events[0].EntityId.ShouldBe("light.after");
    }

    [Fact]
    public async Task SubscribeAsync_CloseFrame_CompletesStreamWithoutException()
    {
        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            await CloseAsync(ws);
        });

        var events = await CollectEventsAsync(BuildSut(server.Port));

        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubscribeAsync_Cancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();

        await using var server = FakeWsServer.Start(async (ws, ct) =>
        {
            await DoAuthAndSubscribeAsync(ws, ct);
            // Cancel the client mid-stream after auth is done
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ct);
        });

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in BuildSut(server.Port).SubscribeAsync(cts.Token))
            { }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HomeAssistantWebSocketClient BuildSut(int port) =>
        new(new SocketsHttpHandler(),
            new HomeAssistantClientOptions { BaseUrl = $"http://localhost:{port}", Token = "test-token" },
            NullLogger<HomeAssistantWebSocketClient>.Instance);

    private static async Task<List<HomeAssistantStateChangedEvent>> CollectEventsAsync(
        HomeAssistantWebSocketClient sut,
        CancellationToken ct = default)
    {
        var list = new List<HomeAssistantStateChangedEvent>();
        await foreach (var ev in sut.SubscribeAsync(ct))
            list.Add(ev);
        return list;
    }

    private static Task SendJson(WebSocket ws, object payload, CancellationToken ct) =>
        ws.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)).AsMemory(),
            WebSocketMessageType.Text, endOfMessage: true, ct).AsTask();

    private static async Task<JsonNode?> ReceiveJson(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        ValueWebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer.AsMemory(), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            ms.Write(buffer.AsSpan(0, result.Count));
        } while (!result.EndOfMessage);
        ms.Seek(0, SeekOrigin.Begin);
        return JsonNode.Parse(ms);
    }

    private static async Task DoAuthAndSubscribeAsync(WebSocket ws, CancellationToken ct)
    {
        await SendJson(ws, new { type = "auth_required" }, ct);
        await ReceiveJson(ws, ct); // auth payload from client
        await SendJson(ws, new { type = "auth_ok" }, ct);
        await ReceiveJson(ws, ct); // subscribe payload from client
        await SendJson(ws, new { success = true }, ct);
    }

    private static Task SendStateChangedEvent(
        WebSocket ws,
        string entityId,
        string newState,
        string oldState,
        CancellationToken ct) =>
        SendJson(ws, new
        {
            type = "event",
            @event = new
            {
                data = new
                {
                    entity_id = entityId,
                    new_state = new { entity_id = entityId, state = newState },
                    old_state = new { entity_id = entityId, state = oldState }
                }
            }
        }, ct);

    private static Task CloseAsync(WebSocket ws) =>
        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

    // ── In-process WebSocket server ───────────────────────────────────────────

    private sealed class FakeWsServer : IAsyncDisposable
    {
        private readonly HttpListener _listener;
        private readonly Task _serverTask;

        public int Port { get; }

        private FakeWsServer(HttpListener listener, int port, Task serverTask)
        {
            _listener = listener;
            Port = port;
            _serverTask = serverTask;
        }

        public static FakeWsServer Start(Func<WebSocket, CancellationToken, Task> protocol)
        {
            var port = FreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                HttpListenerContext ctx;
                try
                { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
                catch { return; }

                HttpListenerWebSocketContext wsCtx;
                try
                { wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false); }
                catch { return; }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                { await protocol(wsCtx.WebSocket, cts.Token).ConfigureAwait(false); }
                catch { /* test controls assertion */ }
            });

            return new FakeWsServer(listener, port, serverTask);
        }

        private static int FreeTcpPort()
        {
            using var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            l.Start();
            return ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            try
            { await _serverTask.ConfigureAwait(false); }
            catch { }
        }
    }
}
