using System;

namespace Lando.Alexa.Security.LWA;

/// <summary>
/// Raised when the LWA token endpoint returns a non-success status or a malformed body.
/// Distinct from generic HTTP exceptions so callers can branch (e.g. AcceptGrantFailed).
/// </summary>
public sealed class LwaTokenException(string message, Exception? inner = null) : Exception(message, inner) { }
