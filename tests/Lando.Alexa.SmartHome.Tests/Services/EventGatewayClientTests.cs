using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq.Protected;
using static Lando.Alexa.Constants.HttpClients;

namespace Lando.Alexa.SmartHome.Services.Tests;

/// <summary>
/// <see cref="EventGatewayClient"/> fans a ChangeReport out to every grantee
/// in <see cref="ITokenStore"/> and aggregates per-recipient success bits.
/// Tests pin URL/method/bearer header, the fan-out + bearer-per-grantee
/// contract, the aggregate-false-on-any-fail rule, transport-exception
/// swallowing, and the retry rules: which outcomes are retried, that every
/// attempt resends the same event, and what each outcome logs.
/// </summary>
public class EventGatewayClientTests
{
    private const string EndpointId = "light#kitchen";
    private const int MaxAttempts = AlexaApi.MaxAttempts;
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Posts_ChangeReport_to_v3_events_with_bearer_token()
    {
        var captured = new System.Collections.Generic.List<(HttpRequestMessage, string)>();
        var handler = NewHandler((req, _) =>
        {
            captured.Add((req, req.Content!.ReadAsStringAsync().Result));
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var store = Store(("amzn1.account.A", "access-A"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        var (sent, body) = captured.ShouldHaveSingleItem();
        sent.Method.ShouldBe(HttpMethod.Post);
        sent.RequestUri!.AbsolutePath.ShouldEndWith("v3/events");
        sent.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        sent.Headers.Authorization.Parameter.ShouldBe("access-A");
        body.ShouldContain(EndpointId);
        body.ShouldContain("powerState");
    }

    [Fact]
    public async Task Fans_out_one_post_per_grantee_with_per_grantee_bearer()
    {
        var captured = new System.Collections.Generic.List<HttpRequestMessage>();
        var handler = NewHandler((req, _) =>
        {
            captured.Add(req);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var store = Store(("amzn1.account.A", "access-A"), ("amzn1.account.B", "access-B"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        captured.Count.ShouldBe(2);
        captured.Select(r => r.Headers.Authorization!.Parameter).ToHashSet()
            .ShouldBe(new System.Collections.Generic.HashSet<string?> { "access-A", "access-B" });
    }

    [Fact]
    public async Task Returns_false_when_any_grantee_fails()
    {
        var i = 0;
        var handler = NewHandler((_, _) =>
            Interlocked.Increment(ref i) == 1
                ? new HttpResponseMessage(HttpStatusCode.Accepted)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var store = Store(("a", "x"), ("b", "y"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeFalse();
    }

    [Fact]
    public async Task Retries_transport_exception_then_swallows_it_and_returns_false()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection reset"));
        var sut = BuildSut(handler.Object, Store(("a", "x")));

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeFalse();
        handler.Protected().Verify(
            "SendAsync", Times.Exactly(MaxAttempts), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Success_on_first_try_sends_once_and_logs_one_information_line()
    {
        var handler = new ScriptedHandler((_, _) => Respond(HttpStatusCode.Accepted));
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log);

        var ok = await Send(sut);

        ok.ShouldBeTrue();
        handler.Bodies.Count.ShouldBe(1);
        handler.ContentTypes.ShouldHaveSingleItem().ShouldBe("application/json; charset=utf-8");
        log.At(LogLevel.Warning).ShouldBeEmpty();
        log.At(LogLevel.Error).ShouldBeEmpty();
        var line = log.SentLine();
        line.ShouldContain(EndpointId);
        line.ShouldContain("attempts 1, status 202");
        line.ShouldEndWith(" ms");
    }

    [Fact]
    public async Task Timeout_then_success_resends_the_same_event()
    {
        var handler = new ScriptedHandler((attempt, ct) => attempt == 1 ? Stall(ct) : Respond(HttpStatusCode.Accepted));
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log);

        var ok = await Send(sut);

        ok.ShouldBeTrue();
        handler.Bodies.Count.ShouldBe(2);
        handler.Bodies[0].ShouldContain("messageId");
        handler.Bodies[1].ShouldBe(handler.Bodies[0]);
        log.At(LogLevel.Warning).ShouldHaveSingleItem().ShouldContain("attempt 1 of 3 failed");
        log.At(LogLevel.Warning).ShouldHaveSingleItem().ShouldContain("status timeout");
        log.At(LogLevel.Error).ShouldBeEmpty();
        log.SentLine().ShouldContain("attempts 2, status 202");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Retryable_status_then_success_resends_the_same_event(HttpStatusCode first)
    {
        var handler = new ScriptedHandler((attempt, _) => Respond(attempt == 1 ? first : HttpStatusCode.Accepted));
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log);

        var ok = await Send(sut);

        ok.ShouldBeTrue();
        handler.Bodies.Count.ShouldBe(2);
        handler.Bodies[1].ShouldBe(handler.Bodies[0]);
        log.At(LogLevel.Warning).ShouldHaveSingleItem().ShouldContain($"status {(int)first}");
        log.At(LogLevel.Error).ShouldBeEmpty();
        log.SentLine().ShouldContain("attempts 2, status 202");
    }

    [Fact]
    public async Task Three_timeouts_lose_the_report_with_one_error()
    {
        var handler = new ScriptedHandler((_, ct) => Stall(ct));
        var log = new RecordingLogger();
        // The client's own Timeout is shorter than the whole sequence (3 x 50 ms + 2 x 1 ms)
        // and must not cut it short: it applies to each request.
        var sut = BuildSut(handler, Store(("a", "x")), log, clientTimeout: TimeSpan.FromMilliseconds(120));

        var ok = await Send(sut);

        ok.ShouldBeFalse();
        handler.Bodies.Count.ShouldBe(MaxAttempts);
        handler.Bodies.Distinct().ShouldHaveSingleItem();
        log.At(LogLevel.Warning).Count.ShouldBe(MaxAttempts);
        log.At(LogLevel.Warning).ShouldAllBe(l => l.Contains("status timeout"));
        log.At(LogLevel.Error).ShouldHaveSingleItem().ShouldContain("attempts 3, status timeout");
        log.At(LogLevel.Information).ShouldNotContain(l => l.Contains("sent:"));
        log.Exceptions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Client_error_is_not_retried(HttpStatusCode status)
    {
        var handler = new ScriptedHandler((_, _) => Respond(status));
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log);

        var ok = await Send(sut);

        ok.ShouldBeFalse();
        handler.Bodies.Count.ShouldBe(1);
        log.At(LogLevel.Warning).ShouldHaveSingleItem().ShouldContain($"status {(int)status}");
        log.At(LogLevel.Error).ShouldHaveSingleItem().ShouldContain($"attempts 1, status {(int)status}");
    }

    [Fact]
    public async Task Caller_cancellation_during_an_attempt_is_not_retried()
    {
        using var caller = new CancellationTokenSource();
        var handler = new ScriptedHandler((_, ct) =>
        {
            caller.Cancel();
            return Stall(ct);
        });
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log, attemptTimeout: TimeSpan.FromSeconds(30));

        var ok = await Send(sut, caller.Token);

        ok.ShouldBeFalse();
        handler.Bodies.Count.ShouldBe(1);
        log.At(LogLevel.Warning).ShouldBeEmpty();
        log.At(LogLevel.Error).ShouldBeEmpty();
        log.At(LogLevel.Information).ShouldContain(l => l.Contains("cancelled: attempts 1"));
    }

    [Fact]
    public async Task Caller_cancellation_between_attempts_stops_the_retries()
    {
        using var caller = new CancellationTokenSource();
        var handler = new ScriptedHandler((_, _) =>
        {
            caller.Cancel();
            return Respond(HttpStatusCode.ServiceUnavailable);
        });
        var log = new RecordingLogger();
        var sut = BuildSut(handler, Store(("a", "x")), log, retryDelay: TimeSpan.FromSeconds(30));

        var ok = await Send(sut, caller.Token);

        ok.ShouldBeFalse();
        handler.Bodies.Count.ShouldBe(1);
        log.At(LogLevel.Error).ShouldBeEmpty();
        log.At(LogLevel.Information).ShouldContain(l => l.Contains("cancelled: attempts 1"));
    }

    [Fact]
    public async Task Returns_true_and_makes_no_calls_when_no_grantees()
    {
        var handler = NewHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Accepted));
        var sut = BuildSut(handler.Object, Store());

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        handler.Protected().Verify(
            "SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> NewHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => respond(req, ct));
        return handler;
    }

    private static ITokenStore Store(params (string userId, string accessToken)[] tokens)
    {
        var store = new Mock<ITokenStore>();
        store.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        return store.Object;
    }

    /// <summary>
    /// Builds the client with test-sized timings (50 ms per attempt, 1 ms between attempts)
    /// so the retry paths run in milliseconds instead of the production 3 s and 1 s.
    /// </summary>
    private static EventGatewayClient BuildSut(
        HttpMessageHandler handler,
        ITokenStore store,
        ILogger<EventGatewayClient>? logger = null,
        TimeSpan? attemptTimeout = null,
        TimeSpan? retryDelay = null,
        TimeSpan? clientTimeout = null)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(AlexaApi.Url),
            Timeout = clientTimeout ?? TimeSpan.FromSeconds(AlexaApi.TimeoutSeconds),
        };
        return new EventGatewayClient(http, store, JsonOptions, logger ?? NullLogger<EventGatewayClient>.Instance)
        {
            AttemptTimeout = attemptTimeout ?? TimeSpan.FromMilliseconds(50),
            RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(1),
        };
    }

    private static ValueTask<bool> Send(EventGatewayClient sut, CancellationToken cancellationToken = default)
        => sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")], [Property("powerState", "ON")], cancellationToken);

    private static Task<HttpResponseMessage> Respond(HttpStatusCode status) => Task.FromResult(new HttpResponseMessage(status));

    /// <summary>
    /// A gateway that accepts the request and never answers: the shape of the 2026-11 stalls.
    /// It ends only when the attempt's token is cancelled.
    /// </summary>
    private static async Task<HttpResponseMessage> Stall(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("unreachable");
    }

    /// <summary>
    /// Answers each attempt from a script keyed on the 1-based attempt number, and records
    /// the body and content type of every request it saw.
    /// </summary>
    private sealed class ScriptedHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> script) : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];

        public List<string?> ContentTypes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            ContentTypes.Add(request.Content.Headers.ContentType?.ToString());
            return await script(Bodies.Count, cancellationToken);
        }
    }

    private sealed class RecordingLogger : ILogger<EventGatewayClient>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public List<Exception> Exceptions { get; } = [];

        public List<string> At(LogLevel level) => [.. _entries.Where(e => e.Level == level).Select(e => e.Message)];

        /// <summary>The one per-send summary line a successful send writes.</summary>
        public string SentLine() => At(LogLevel.Information).Where(m => m.Contains("sent:")).ShouldHaveSingleItem();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _entries.Add((logLevel, formatter(state, exception)));
            if (exception is not null)
            {
                Exceptions.Add(exception);
            }
        }
    }

    private static ContextProperty Property(string name, object value) => new()
    {
        Namespace = Namespaces.PowerController,
        Name = name,
        Value = value,
        TimeOfSample = DateTime.UtcNow,
    };
}
