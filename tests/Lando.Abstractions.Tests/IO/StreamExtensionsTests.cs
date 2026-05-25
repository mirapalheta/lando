using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lando.IO.Tests;

public class StreamExtensionsTests
{
    // ── CopyToAsync (maxBytes) ────────────────────────────────────────────────

    [Fact]
    public async Task CopyToAsync_WithinLimit_CopiesAllBytes()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        using var source = new MemoryStream(data);
        using var dest = new MemoryStream();

        await source.CopyToAsync(dest, maxBytes: 1024);

        dest.ToArray().ShouldBe(data);
    }

    [Fact]
    public async Task CopyToAsync_ExactlyAtLimit_CopiesAllBytes()
    {
        var data = new byte[8192];
        new Random(42).NextBytes(data);
        using var source = new MemoryStream(data);
        using var dest = new MemoryStream();

        await source.CopyToAsync(dest, maxBytes: 8192);

        dest.ToArray().ShouldBe(data);
    }

    [Fact]
    public async Task CopyToAsync_ExceedsLimit_ThrowsLandoExceptionWith413()
    {
        var data = new byte[100];
        using var source = new MemoryStream(data);
        using var dest = new MemoryStream();

        var ex = await Should.ThrowAsync<LandoException>(
            () => source.CopyToAsync(dest, maxBytes: 50));

        ex.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task CopyToAsync_ExceedsLimit_MessageMentionsBudget()
    {
        var data = new byte[200];
        using var source = new MemoryStream(data);
        using var dest = new MemoryStream();

        var ex = await Should.ThrowAsync<LandoException>(
            () => source.CopyToAsync(dest, maxBytes: 100));

        ex.Message.ShouldContain("100");
    }

    [Fact]
    public async Task CopyToAsync_ExceedsLimit_DoesNotDrainFullSource()
    {
        // Verifies early-exit: source must still have unread bytes after throw.
        // We use a large payload so that the rejection happens within the first chunk.
        var data = new byte[16_384];
        using var source = new MemoryStream(data);
        using var dest = new MemoryStream();

        await Should.ThrowAsync<LandoException>(
            () => source.CopyToAsync(dest, maxBytes: 100));

        // Source position should be well short of its full length.
        source.Position.ShouldBeLessThan(data.Length);
    }

    [Fact]
    public async Task CopyToAsync_EmptySource_WritesNothing()
    {
        using var source = new MemoryStream();
        using var dest = new MemoryStream();

        await source.CopyToAsync(dest, maxBytes: 1024);

        dest.Length.ShouldBe(0);
    }

    [Fact]
    public async Task CopyToAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Use a large payload so the cancel fires mid-copy.
        var data = new byte[64_000];
        using var source = new SlowStream(data, chunkSize: 1024);
        using var dest = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => source.CopyToAsync(dest, maxBytes: long.MaxValue, cts.Token));
    }

    // ── AsSpan ────────────────────────────────────────────────────────────────

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
        using var ms = new MemoryStream(capacity: 1024);
        ms.Write(Encoding.UTF8.GetBytes("short"));

        var span = ms.AsSpan();

        span.Length.ShouldBe(5);
        Encoding.UTF8.GetString(span).ShouldBe("short");
    }

    [Fact]
    public void AsSpan_OnNonMemoryStream_BuffersAndReturnsExactBytes()
    {
        var expected = Encoding.UTF8.GetBytes("non-memory-stream payload");
        using var source = new MemoryStream(expected);
        using var wrapped = new BufferedStream(source);

        var span = wrapped.AsSpan();

        span.ToArray().ShouldBe(expected);
    }

    [Fact]
    public void AsSpan_OnEmptyMemoryStream_ReturnsEmptySpan()
    {
        using var ms = new MemoryStream();

        var span = ms.AsSpan();

        span.IsEmpty.ShouldBeTrue();
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_OnSeekableStream_RewindsPositionToZero()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        ms.Position = 2;

        var returned = ms.Reset();

        returned.ShouldBeSameAs(ms);
        ms.Position.ShouldBe(0L);
    }

    [Fact]
    public void Reset_OnAlreadyRewindedStream_IsIdempotent()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        ms.Reset();
        ms.Reset();

        ms.Position.ShouldBe(0L);
    }

    [Fact]
    public void Reset_PreservesStreamContents()
    {
        var data = Encoding.UTF8.GetBytes("preserve me");
        using var ms = new MemoryStream(data);
        ms.Position = ms.Length;

        ms.Reset();

        ms.ToArray().ShouldBe(data);
    }

    [Fact]
    public void Reset_OnNonSeekableStream_DoesNotThrow()
    {
        using var inner = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        using var nonSeekable = new NonSeekableStream(inner);

        Should.NotThrow(() => nonSeekable.Reset());
    }

    [Fact]
    public void Reset_ReturnsOriginalInstance_PreservingConcreteType()
    {
        using var ms = new MemoryStream();

        MemoryStream returned = ms.Reset();

        returned.ShouldBeSameAs(ms);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Wraps a byte array and exposes reads one chunk at a time, making cancellation observable.</summary>
    private sealed class SlowStream(byte[] data, int chunkSize) : Stream
    {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => data.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= data.Length)
                return 0;
            var toCopy = Math.Min(Math.Min(count, chunkSize), data.Length - _position);
            Array.Copy(data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Read(buffer.Span));
        }

        public override int Read(Span<byte> buffer)
        {
            if (_position >= data.Length)
                return 0;
            var toCopy = Math.Min(Math.Min(buffer.Length, chunkSize), data.Length - _position);
            data.AsSpan(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
            return toCopy;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }
}
