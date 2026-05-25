using System;

namespace Lando.HomeAssistant.Exceptions;

/// <summary>
/// Thrown when an operation against the Home Assistant REST or WebSocket API fails.
/// Wraps lower-level transport, authentication, or deserialization exceptions so callers
/// can handle a single domain-level failure type instead of branching on every transport
/// variant. The inner exception preserves the original cause for diagnostics.
/// </summary>
public class HomeAssistantException(string message, Exception? innerException = null) : Exception(message, innerException) { }
