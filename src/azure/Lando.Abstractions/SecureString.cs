using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Lando;

/// <summary>
/// String wrapper that participates in an ambient "redaction" context: when
/// the surrounding scope has redaction enabled (e.g. while a request/response
/// is being JSON-serialised for logging), the JSON converter substitutes the
/// value with <c>"**********"</c> instead of emitting it on the wire.
/// </summary>
/// <remarks>
/// <para>
/// Use this for any field that holds a bearer token, OAuth code, signing key,
/// or other secret that flows through the request/response pipeline. The
/// model classes declare these fields as <see cref="SecureString"/>; the
/// logging middleware brackets serialisation with
/// <see cref="WithRedactionEnabled()"/>; the production serialiser leaves
/// redaction off so the real value flows over the wire.
/// </para>
/// <para>
/// Redaction state is stored in an <see cref="AsyncLocal{T}"/>, so concurrent
/// requests on different async flows do not see each other's redaction mode.
/// </para>
/// </remarks>
[JsonConverter(typeof(SecureStringConverter))]
public readonly struct SecureString(string? value) : IEquatable<SecureString>
{
    // AsyncLocal keeps context isolated to the current execution thread/async flow
    private static readonly AsyncLocal<bool> IsLoggingContext = new();

    /// <summary>
    /// The unwrapped underlying value.
    /// </summary>
    public string? Value => value;

    /// <summary>
    /// Globally turns on redaction for the current async flow.
    /// </summary>
    public static void EnableRedaction() => IsLoggingContext.Value = true;

    /// <summary>
    /// Globally turns off redaction for the current async flow.
    /// </summary>
    public static void DisableRedaction() => IsLoggingContext.Value = false;

    /// <summary>
    /// Whether redaction is currently enabled on this async flow.
    /// </summary>
    public static bool IsRedactionEnabled => IsLoggingContext.Value;

    /// <summary>
    /// Turns redaction on for the duration of the returned scope. The previous
    /// redaction state is restored when the result is disposed — typical use
    /// is <c>using var _ = SecureString.WithRedactionEnabled();</c>.
    /// </summary>
    /// <returns>A scope object that restores prior redaction state on dispose.</returns>
    public static IDisposable WithRedactionEnabled()
    {
        var previous = IsLoggingContext.Value;
        IsLoggingContext.Value = true;
        return new DisposableAction(() => IsLoggingContext.Value = previous);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with redaction enabled, restoring the
    /// prior state regardless of how <paramref name="action"/> exits.
    /// </summary>
    /// <param name="action">The action to invoke under redaction.</param>
    public static void WithRedactionEnabled(Action action)
    {
        using var _ = WithRedactionEnabled();
        action();
    }

    /// <summary>
    /// Runs <paramref name="func"/> with redaction enabled and returns its
    /// result, restoring the prior redaction state regardless of how
    /// <paramref name="func"/> exits.
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="func"/>.</typeparam>
    /// <param name="func">The function to invoke under redaction.</param>
    /// <returns>The value returned by <paramref name="func"/>.</returns>
    public static T WithRedactionEnabled<T>(Func<T> func)
    {
        using var _ = WithRedactionEnabled();
        return func();
    }

    // // Implicit conversions to make it transparent to your application
    // public static implicit operator SecureString(string? value) => new(value);
    // public static implicit operator string(SecureString secureStr) => secureStr.Value!;

    /// <inheritdoc/>
    public override string? ToString() => Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SecureString other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SecureString other) => Value == other.Value;

    /// <inheritdoc/>
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    /// <summary>
    /// Equality operator over the wrapped string value.
    /// </summary>
    public static bool operator ==(SecureString left, SecureString right) => left.Equals(right);

    /// <summary>
    /// Inequality operator over the wrapped string value.
    /// </summary>
    public static bool operator !=(SecureString left, SecureString right) => !left.Equals(right);

    // The dedicated converter
    private class SecureStringConverter : JsonConverter<SecureString>
    {
        public override SecureString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, SecureString value, JsonSerializerOptions options)
            => writer.WriteStringValue(IsLoggingContext.Value ? "**********" : value.Value);
    }
}
