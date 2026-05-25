using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.FunctionApp.Functions;

public class FunctionBaseTests
{
    private const string Key = "test-key";

    // Must be public — Castle DynamicProxy (Moq) can't proxy IRequestHandler<T,R>
    // when the type arguments are private nested types.
    public record TestRequest(string Value);
    public record TestResponse(string Result);

    // ── SUT wrapper ────────────────────────────────────────────────────────────

    private sealed class TestFunction : FunctionBase<TestRequest, TestResponse>
    {
        public Task<HttpResponseData> InvokeAsync(
            string key, HttpRequestData req, FunctionContext ctx, CancellationToken ct)
            => HandleRequestAsync(key, req, ctx, ct);
    }

    // ── Fakes ──────────────────────────────────────────────────────────────────

    private sealed class FakeHttpRequestData(FunctionContext ctx, Stream? body) : HttpRequestData(ctx)
    {
        public override Stream Body => body!;
        public override HttpHeadersCollection Headers { get; } = new();
        public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
        public override Uri Url => new("https://localhost/api");
        public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
        public override string Method => "POST";
        public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
    }

    private sealed class FakeHttpResponseData(FunctionContext ctx) : HttpResponseData(ctx)
    {
        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = new();
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies => null!;
    }

    // IRequestValidator.Validate takes ReadOnlySpan<byte> — Moq cannot match ref structs,
    // so use hand-written fakes.
    private sealed class PassValidator : IRequestValidator
    {
        public bool Validate(HttpHeaders h, ReadOnlySpan<byte> b, string c) => true;
    }

    private sealed class ThrowingValidator(LandoException ex) : IRequestValidator
    {
        public bool Validate(HttpHeaders h, ReadOnlySpan<byte> b, string c) => throw ex;
    }

    // A stream that pretends to contain `size` zero bytes without allocating them.
    private sealed class BigStream(long size) : Stream
    {
        private long _read;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => size;
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            long remaining = size - _read;
            if (remaining <= 0)
                return 0;
            int n = (int)Math.Min(count, remaining);
            Array.Clear(buffer, offset, n);
            _read += n;
            return n;
        }
        public override void Flush() { }
        public override long Seek(long o, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Mock<FunctionContext> BuildContext(
        IRequestValidator validator,
        Mock<IRequestHandler<TestRequest, TestResponse>> handler,
        string key = Key)
    {
        var provider = new Mock<IKeyedServiceProvider>();
        var options = Options.Create(new JsonSerializerOptions());

        var serializer = Mock.Of<Azure.Core.Serialization.ObjectSerializer>();

        // GetService(typeof(IOptions<WorkerOptions>)) → used by Functions SDK to get the configured serializer 
        // for deserialising request bodies (see WorkerOptions.Serializer).
        provider.Setup(p => p.GetService(typeof(IOptions<WorkerOptions>)))
            .Returns(Options.Create(new WorkerOptions { Serializer = serializer }));

        // GetLogger<T>() calls context.InstanceServices.GetService<ILoggerFactory>()
        provider.Setup(p => p.GetService(typeof(ILogger<FunctionBase<TestRequest, TestResponse>>)))
            .Returns(NullLogger<FunctionBase<TestRequest, TestResponse>>.Instance);

        // GetRequiredService<IOptions<JsonSerializerOptions>>() → GetService(typeof(...))
        provider.Setup(p => p.GetService(typeof(IOptions<JsonSerializerOptions>))).Returns(options);

        // GetRequiredKeyedService<IRequestValidator>(key) calls GetRequiredKeyedService(Type, object?)
        provider.Setup(p => p.GetRequiredKeyedService(typeof(IRequestValidator), key))
            .Returns(validator);

        // GetRequiredKeyedService<IRequestHandler<TReq,TRes>>(key)
        provider.Setup(p => p.GetRequiredKeyedService(typeof(IRequestHandler<TestRequest, TestResponse>), key))
            .Returns(handler.Object);

        var ctx = new Mock<FunctionContext>();
        ctx.Setup(c => c.InstanceServices).Returns(provider.Object);
        return ctx;
    }

    private static Stream JsonBody(TestRequest req)
        => new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req)));

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleRequestAsync_ValidRequest_Returns200()
    {
        var handler = new Mock<IRequestHandler<TestRequest, TestResponse>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResponse("ok"));

        var ctx = BuildContext(new PassValidator(), handler);
        var req = new FakeHttpRequestData(ctx.Object, JsonBody(new TestRequest("hello")));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HandleRequestAsync_DispatchesDeserializedRequest()
    {
        TestRequest? captured = null;
        var handler = new Mock<IRequestHandler<TestRequest, TestResponse>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new TestResponse("ok"));

        var ctx = BuildContext(new PassValidator(), handler);
        var req = new FakeHttpRequestData(ctx.Object, JsonBody(new TestRequest("payload-value")));

        await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        captured.ShouldNotBeNull();
        captured.Value.ShouldBe("payload-value");
    }

    [Fact]
    public async Task HandleRequestAsync_NullBody_ReturnsBadRequest()
    {
        var ctx = BuildContext(new PassValidator(), new Mock<IRequestHandler<TestRequest, TestResponse>>());
        var req = new FakeHttpRequestData(ctx.Object, body: null);

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleRequestAsync_NullJsonBody_ReturnsBadRequest()
    {
        // "null" deserialises to null for a reference type → ?? throw LandoException(BadRequest)
        var ctx = BuildContext(new PassValidator(), new Mock<IRequestHandler<TestRequest, TestResponse>>());
        var req = new FakeHttpRequestData(ctx.Object, new MemoryStream("null"u8.ToArray()));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleRequestAsync_BodyExceedsLimit_Returns413()
    {
        var ctx = BuildContext(new PassValidator(), new Mock<IRequestHandler<TestRequest, TestResponse>>());
        var req = new FakeHttpRequestData(ctx.Object, new BigStream(6L * 1024 * 1024 + 1));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task HandleRequestAsync_ValidatorThrowsLandoException_ReturnsMatchingStatus()
    {
        var ctx = BuildContext(
            new ThrowingValidator(new LandoException(HttpStatusCode.Unauthorized, "bad sig")),
            new Mock<IRequestHandler<TestRequest, TestResponse>>());
        var req = new FakeHttpRequestData(ctx.Object, JsonBody(new TestRequest("x")));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleRequestAsync_HandlerThrowsLandoException_ReturnsMatchingStatus()
    {
        var handler = new Mock<IRequestHandler<TestRequest, TestResponse>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LandoException(HttpStatusCode.UnprocessableEntity, "bad data"));

        var ctx = BuildContext(new PassValidator(), handler);
        var req = new FakeHttpRequestData(ctx.Object, JsonBody(new TestRequest("x")));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task HandleRequestAsync_HandlerThrowsUnexpectedException_Returns500()
    {
        var handler = new Mock<IRequestHandler<TestRequest, TestResponse>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ctx = BuildContext(new PassValidator(), handler);
        var req = new FakeHttpRequestData(ctx.Object, JsonBody(new TestRequest("x")));

        var result = await new TestFunction().InvokeAsync(Key, req, ctx.Object, default);

        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
