using System;
using System.Net;
using System.Net.Http.Headers;

namespace Lando;

/// <summary>
/// Authenticates an inbound request before it is parsed or dispatched.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to throw a <see cref="LandoException"/>
/// (typically with <see cref="HttpStatusCode.Unauthorized"/>) on
/// failure rather than returning <see langword="false"/>. The boolean return
/// exists for symmetry with future validators that may want to express a
/// soft "not for me" result, but today every shipped validator throws.
/// </para>
/// <para>
/// The body is passed as a <see cref="ReadOnlySpan{T}"/> over already-buffered
/// bytes. Callers — typically <c>FunctionBase&lt;TRequest,TResponse&gt;</c> —
/// are responsible for draining and length-capping the body stream before
/// invoking <see cref="Validate"/>. The synchronous shape is deliberate:
/// <see cref="ReadOnlySpan{T}"/> is a ref struct and cannot cross
/// <see langword="await"/> boundaries, so any implementation that needs
/// async I/O must buffer first and validate over the bytes — the same
/// pattern callers already use to invoke this method.
/// </para>
/// </remarks>
public interface IRequestValidator
{
    /// <summary>
    /// Validates an inbound request. Returns <see langword="true"/> on
    /// success; throws on failure.
    /// </summary>
    /// <param name="headers">HTTP headers from the inbound request.</param>
    /// <param name="body">Already-buffered raw bytes of the request body.</param>
    /// <param name="correlationId">
    /// Correlation identifier generated (or extracted from a header) by the
    /// caller, used for structured logging.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the request is authentic. Shipped
    /// implementations never return <see langword="false"/> — they throw
    /// instead (see remarks on the interface).
    /// </returns>
    bool Validate(HttpHeaders headers, ReadOnlySpan<byte> body, string correlationId);
}
