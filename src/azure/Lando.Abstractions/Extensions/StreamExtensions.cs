using System.Buffers;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Lando;

namespace System.IO;

/// <summary>
/// Stream helpers used by request-processing paths that need to inspect the
/// body bytes (for HMAC verification, signature replay, etc.) while still
/// handing a usable <see cref="Stream"/> to downstream deserialisers.
/// </summary>
public static class StreamExtensions
{
    extension(Stream stream)
    {
        /// <summary>
        /// Copies <paramref name="stream"/> into <paramref name="destination"/>
        /// up to <paramref name="maxBytes"/>; throws
        /// <see cref="LandoException"/> with
        /// <see cref="HttpStatusCode.RequestEntityTooLarge"/> as soon as the
        /// budget is exceeded, without draining the rest of the source.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this in place of <c>Stream.CopyToAsync</c> when the source is a
        /// forward-only HTTP body that does not support <see cref="Stream.Length"/>
        /// (e.g. the Azure Functions worker's <c>HttpRequestStream</c>). The
        /// per-chunk check means a 10 GB attacker payload is rejected after the
        /// first 8 KiB read, not after the full body has been buffered.
        /// </para>
        /// <para>
        /// Reads use a pooled buffer to avoid allocation churn under load.
        /// </para>
        /// </remarks>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="maxBytes">
        /// Maximum number of bytes to copy before throwing. Counted as the
        /// running total of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="LandoException">
        /// Thrown with <see cref="HttpStatusCode.RequestEntityTooLarge"/> if the
        /// source produces more than <paramref name="maxBytes"/> bytes.
        /// </exception>
        public async Task CopyToAsync(Stream destination, long maxBytes, CancellationToken cancellationToken = default)
        {
            var pool = ArrayPool<byte>.Shared;
            var buffer = pool.Rent(8192);
            try
            {
                long total = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                    {
                        throw new LandoException(
                            HttpStatusCode.RequestEntityTooLarge,
                            $"Request body exceeds the {maxBytes:N0}-byte limit.");
                    }
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{T}"/> over the stream's bytes. If
        /// <paramref name="stream"/> is already a <see cref="MemoryStream"/>, the
        /// span aliases its underlying buffer (zero copies). Otherwise the
        /// stream is drained into a temporary <see cref="MemoryStream"/> and the
        /// span is returned over that buffer — the temporary lives only for the
        /// lifetime of the buffer it produced; do not retain the span past the
        /// surrounding stack frame.
        /// </summary>
        /// <remarks>
        /// Intended for callers that need the bytes *now*, in-place, to feed a
        /// synchronous API like
        /// <see cref="IncrementalHash.AppendData(ReadOnlySpan{byte})"/>.
        /// Spans cannot cross <c>await</c> boundaries, so the caller is
        /// responsible for completing any span-consuming work synchronously.
        /// </remarks>
        public ReadOnlySpan<byte> AsSpan()
        {
            if (stream is MemoryStream ms)
            {
                var buffer = ms.TryGetBuffer(out var segment) ? segment : ms.ToArray();
                return buffer.AsSpan(0, (int)ms.Length);
            }

            // For non-MemoryStream sources we buffer into a fresh MemoryStream.
            // The buffer survives the MemoryStream's Dispose because the
            // returned span keeps a GC reference to it.
            using var buffered = new MemoryStream();
            stream.Reset().CopyTo(buffered);
            return buffered.AsSpan();
        }
    }

    /// <summary>
    /// Rewinds <paramref name="stream"/> to position 0 if it supports
    /// seeking; otherwise a no-op. Returns <paramref name="stream"/> for
    /// chaining — handy when re-using a buffered request body for both
    /// validation and deserialisation in the same pipeline.
    /// </summary>
    /// <typeparam name="T">The concrete stream type, preserved through the chain.</typeparam>
    /// <param name="stream">The stream to rewind.</param>
    /// <returns>The same <paramref name="stream"/> instance.</returns>
    public static T Reset<T>(this T stream)
        where T : Stream
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        return stream;
    }
}
