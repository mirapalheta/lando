#pragma warning disable CS0419 // Ambiguous reference in cref attribute

using System.IO;
using System.Text;

namespace Lando.Alexa.Core.IO.Tests;

/// <summary>
/// Coverage for <see cref="StreamExtensions"/>, which sits in the inbound
/// HMAC path. A regression here would silently feed the verifier the wrong
/// bytes — that's how the original "every request 401s" bug happened — so
/// these tests assert both the zero-copy MemoryStream branch and the
/// non-MemoryStream buffering branch produce bytes identical to the input.
/// </summary>
public class StreamExtensionsTests
{
    [Fact]
    public void AsSpan_OnMemoryStream_ReturnsExactBytes()
    {
        var expected = Encoding.UTF8.GetBytes("hello world");
        using var ms = new MemoryStream(expected);

        var span = ms.AsSpan();

        span.ToArray().ShouldBe(expected);
    }

    [Fact]
    public void AsSpan_OnMemoryStreamWithCapacityHeadroom_OnlyReturnsWrittenBytes()
    {
        // GetBuffer() can return a backing array larger than Length.
        // AsSpan must slice to Length so callers don't hash trailing garbage.
        using var ms = new MemoryStream(capacity: 1024);
        ms.Write(Encoding.UTF8.GetBytes("short"));

        var span = ms.AsSpan();

        span.Length.ShouldBe(5);
        Encoding.UTF8.GetString(span).ShouldBe("short");
    }

    [Fact]
    public void AsSpan_OnNonMemoryStream_BuffersAndReturnsExactBytes()
    {
        var expected = Encoding.UTF8.GetBytes("""{"directive":{"header":{"namespace":"Alexa"}}}""");
        // BufferedStream over a MemoryStream still reports as not-a-MemoryStream
        // to AsSpan, exercising the buffering branch.
        using var source = new MemoryStream(expected);
        using var wrapped = new BufferedStream(source);

        var span = wrapped.AsSpan();

        span.ToArray().ShouldBe(expected);
    }

    [Fact]
    public void AsSpan_OnEmptyStream_ReturnsEmptySpan()
    {
        using var ms = new MemoryStream();

        var span = ms.AsSpan();

        span.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Reset_OnSeekableStream_RewindsToZero()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        ms.Position = 2;

        var returned = ms.Reset();

        returned.ShouldBeSameAs(ms);
        ms.Position.ShouldBe(0);
    }

    [Fact]
    public void Reset_OnNonSeekableStream_DoesNotThrow()
    {
        // We never *get* a non-seekable stream from the Functions worker in
        // practice, but the helper should still degrade gracefully.
        var data = Encoding.UTF8.GetBytes("abc");
        using var inner = new MemoryStream(data);
        using var nonSeekable = new NonSeekableStream(inner);

        Should.NotThrow(() => nonSeekable.Reset());
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new System.NotSupportedException();
        public override long Position
        {
            get => throw new System.NotSupportedException();
            set => throw new System.NotSupportedException();
        }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new System.NotSupportedException();
        public override void SetLength(long value) => throw new System.NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }
}
