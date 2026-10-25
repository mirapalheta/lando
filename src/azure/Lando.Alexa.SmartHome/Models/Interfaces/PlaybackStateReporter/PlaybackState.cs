using System;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PlaybackStateReporter;

/// <summary>
/// Value object reported for the <c>Alexa.PlaybackStateReporter.playbackState</c>
/// property, serialised as <c>{ "state": "PLAYING" }</c>.
/// </summary>
/// <remarks>
/// Implements value equality so <c>ChangeReportService</c>'s property diff compares
/// it directly instead of falling back to JSON serialisation.
/// </remarks>
public sealed class PlaybackState : IEquatable<PlaybackState>
{
    /// <summary>
    /// Playback state — one of <see cref="PlaybackStateValue"/>.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = PlaybackStateValue.Stopped;

    /// <inheritdoc />
    public bool Equals(PlaybackState? other)
        => other is not null && string.Equals(State, other.State, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PlaybackState);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(State);
}
