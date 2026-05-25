using System;
using System.Net;

namespace Lando;

/// <summary>
/// The bridge's transport-aware exception type. Carries an
/// <see cref="HttpStatusCode"/> so that <c>FunctionBase&lt;TRequest,TResponse&gt;</c>
/// can translate a failed request into the right HTTP response without each
/// caller having to map error categories back to status codes.
/// </summary>
/// <remarks>
/// Throw this from validators and request handlers whenever a failure has a
/// clear HTTP semantic (401 on signature failure, 413 on oversize body, 502 on
/// upstream Home Assistant unavailability, and so on). Reserve plain
/// <see cref="Exception"/> for genuinely unexpected programming errors that
/// should surface as 500s.
/// </remarks>
public class LandoException : Exception
{
    /// <summary>
    /// Initialises a new <see cref="LandoException"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code that should be returned to the caller.</param>
    /// <param name="message">A human-readable description of the failure for logs and developers.</param>
    /// <param name="innerException">Optional underlying exception that triggered this failure.</param>
    public LandoException(HttpStatusCode statusCode, string message, Exception? innerException = default) : base(message, innerException)
        => StatusCode = statusCode;

    /// <summary>
    /// HTTP status code to be returned to the caller when this exception bubbles
    /// out of a request handler.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}
