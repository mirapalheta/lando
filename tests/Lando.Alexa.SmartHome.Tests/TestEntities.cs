using System.Collections.Generic;
using System.Text.Json;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Transformers.Entity;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Per-domain factories for <see cref="HomeAssistantEntity"/> fixtures. Centralises
/// the attribute-bag shape so individual tests stay focused on the behaviour they
/// assert on rather than restating the JSON-element wrapping each entity needs.
/// </summary>
/// <remarks>
/// HA serialises attribute values as JSON; after deserialisation they land in the
/// entity's <c>attributes</c> bag as <see cref="JsonElement"/> instances. To
/// exercise the same code paths consumers hit in production, the helpers below
/// round-trip each value through <see cref="JsonSerializer"/> before stuffing it
/// into the bag.
/// </remarks>
internal static class TestEntities
{
    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>light</c> domain with the
    /// supplied state and attributes; defaults to an on/off-only bulb.
    /// </summary>
    /// <remarks>
    /// The <c>supportedColorModes</c> parameter drives discovery's capability
    /// choices, so tests opt into Brightness, ColorTemperature, or Color
    /// controllers by passing the matching mode strings.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string (<c>"on"</c> or <c>"off"</c>).</param>
    /// <param name="supportedColorModes">The light's color modes; defaults to <c>["onoff"]</c>.</param>
    /// <param name="brightness255">Optional brightness on the HA 0..255 scale.</param>
    /// <param name="colorTempMired">Optional colour temperature in mired.</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <param name="hs_color">Optional hue/saturation color as a two-element array [hue, saturation].</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Light(
        string entityId = "light.living_room",
        string state = "on",
        IReadOnlyList<string>? supportedColorModes = null,
        int? brightness255 = null,
        int? colorTempMired = null,
        bool exposed = true,
        string[]? hs_color = null) => Entity(
            entityId,
            state,
            (EntityAttributes.SupportedColorModes, (object?)(supportedColorModes ?? ["onoff"])),
            (EntityAttributes.Brightness, brightness255),
            (EntityAttributes.ColorTemp, colorTempMired),
            (CustomAttributes.Expose, exposed),
            ("hs_color", hs_color));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>switch</c> domain.
    /// </summary>
    /// <remarks>
    /// Switches have no domain-specific attributes — only the exposure flag is
    /// surfaced — so the parameter list is intentionally small.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string.</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Switch(
        string entityId = "switch.outlet",
        string state = "on",
        bool exposed = true) => Entity(
            entityId,
            state,
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>cover</c> domain with
    /// configurable <c>device_class</c>, supported-features bitmask, and current
    /// position.
    /// </summary>
    /// <remarks>
    /// The <c>deviceClass</c> argument drives both display category and the
    /// shade-vs-binary branch in
    /// <see cref="CoverDiscoveryTransformer"/>; tests cover both
    /// branches through this single factory.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="deviceClass">The HA <c>device_class</c> for the cover.</param>
    /// <param name="supportedFeatures">The HA supported-features bitmask.</param>
    /// <param name="state">The HA state string.</param>
    /// <param name="currentPosition">Optional current position 0..100.</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Cover(
        string entityId = "cover.living_room",
        string? deviceClass = "shade",
        int supportedFeatures = CoverFeatures.Open | CoverFeatures.Close | CoverFeatures.SetPosition,
        string state = "open",
        int? currentPosition = 50,
        bool exposed = true) => Entity(
            entityId,
            state,
            (EntityAttributes.DeviceClass, deviceClass),
            (EntityAttributes.SupportedFeatures, supportedFeatures),
            (EntityAttributes.CurrentPosition, currentPosition),
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>fan</c> domain.
    /// </summary>
    /// <remarks>
    /// The <c>SET_SPEED</c> bit on <paramref name="supportedFeatures"/> is what
    /// causes the discovery transformer to attach
    /// <see cref="Capability.FanSpeed"/> alongside
    /// PowerController.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="supportedFeatures">The HA supported-features bitmask.</param>
    /// <param name="state">The HA state string.</param>
    /// <param name="percentage">Optional current speed percent.</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Fan(
        string entityId = "fan.bedroom",
        int supportedFeatures = FanFeatures.SetSpeed,
        string state = "on",
        int? percentage = 66,
        bool exposed = true) => Entity(
            entityId,
            state,
            (EntityAttributes.SupportedFeatures, supportedFeatures),
            (EntityAttributes.Percentage, percentage),
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>climate</c> domain.
    /// </summary>
    /// <remarks>
    /// The HA state string doubles as the HVAC mode here (for example
    /// <c>"heat"</c>, <c>"cool"</c>), which lines up with how the climate state
    /// transformer reads it.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HVAC mode string.</param>
    /// <param name="currentTemp">Optional current temperature.</param>
    /// <param name="targetTemp">Optional target setpoint.</param>
    /// <param name="unit">The temperature unit (for example <c>"°F"</c>).</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Climate(
        string entityId = "climate.living_room",
        string state = "heat",
        double? currentTemp = 70d,
        double? targetTemp = 72d,
        string unit = "°F",
        bool exposed = true) => Entity(
            entityId,
            state,
            (EntityAttributes.UnitOfMeasurement, unit),
            (EntityAttributes.SupportedFeatures, ClimateFeatures.TargetTemperature),
            (EntityAttributes.CurrentTemperature, currentTemp),
            (EntityAttributes.Temperature, targetTemp),
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>media_player</c>
    /// domain.
    /// </summary>
    /// <remarks>
    /// Volume in HA is 0..1, not 0..100 — the state transformer is responsible
    /// for the conversion, so tests pass the HA-native value here.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="deviceClass">The HA <c>device_class</c> (<c>"tv"</c>, <c>"speaker"</c>, etc.).</param>
    /// <param name="supportedFeatures">The HA supported-features bitmask.</param>
    /// <param name="state">The HA state string.</param>
    /// <param name="volumeLevel">Optional volume on the HA 0..1 scale.</param>
    /// <param name="isVolumeMuted">Optional mute flag.</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity MediaPlayer(
        string entityId = "media_player.living_room_tv",
        string? deviceClass = "tv",
        int supportedFeatures = MediaPlayerFeatures.VolumeSet | MediaPlayerFeatures.TurnOn | MediaPlayerFeatures.TurnOff,
        string state = "playing",
        double? volumeLevel = 0.5,
        bool? isVolumeMuted = false,
        bool exposed = true) => Entity(
            entityId,
            state,
            (EntityAttributes.DeviceClass, deviceClass),
            (EntityAttributes.SupportedFeatures, supportedFeatures),
            (EntityAttributes.VolumeLevel, volumeLevel),
            (EntityAttributes.IsVolumeMuted, isVolumeMuted),
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>sensor</c> domain.
    /// </summary>
    /// <remarks>
    /// The sensor reading is carried in <c>State</c> as a numeric string,
    /// matching the HA REST API shape where sensor entities store their current
    /// value directly on the state field.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="deviceClass">The HA <c>device_class</c> (<c>"temperature"</c> or <c>"humidity"</c>).</param>
    /// <param name="state">The numeric sensor reading as a string (e.g. <c>"23.5"</c>).</param>
    /// <param name="unit">The <c>unit_of_measurement</c> (e.g. <c>"°F"</c>, <c>"%"</c>).</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Sensor(
        string entityId = "sensor.living_room_temp",
        string deviceClass = "temperature",
        string state = "72.0",
        string? unit = "°F",
        bool exposed = true) => Entity(
            entityId,
            state,
            (EntityAttributes.DeviceClass, deviceClass),
            (EntityAttributes.UnitOfMeasurement, unit),
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>lock</c> domain.
    /// </summary>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string (<c>"locked"</c>, <c>"unlocked"</c>, <c>"jammed"</c>, etc.).</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Lock(
        string entityId = "lock.front_door",
        string state = "locked",
        bool exposed = true) => Entity(
            entityId,
            state,
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>scene</c> domain. HA
    /// scenes carry their last-activated timestamp as state; the value is
    /// irrelevant to SceneController discovery, which is stateless.
    /// </summary>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string (a timestamp in practice).</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Scene(
        string entityId = "scene.movie_night",
        string state = "2026-01-01T00:00:00+00:00",
        bool exposed = true) => Entity(
            entityId,
            state,
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Builds a <see cref="HomeAssistantEntity"/> for the <c>script</c> domain.
    /// Scripts report <c>on</c> while running and <c>off</c> otherwise; the value
    /// is irrelevant to SceneController discovery, which is stateless.
    /// </summary>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string (<c>"on"</c>/<c>"off"</c>).</param>
    /// <param name="exposed">Whether the entity is marked <c>lando_expose=true</c>.</param>
    /// <returns>The fabricated entity.</returns>
    public static HomeAssistantEntity Script(
        string entityId = "script.wake_up",
        string state = "off",
        bool exposed = true) => Entity(
            entityId,
            state,
            (CustomAttributes.Expose, exposed));

    /// <summary>
    /// Generic factory for payload-transformer tests that only care about the
    /// entity id (and optionally a single attribute the transformer reads).
    /// Picks the matching per-domain factory based on the entity id's prefix.
    /// </summary>
    /// <remarks>
    /// Lets <see cref="Transformers.Payload"/> tests declare an entity in one
    /// line — most transformers only branch on domain. Tests that need
    /// richer attributes should still use the per-domain factories.
    /// </remarks>
    public static HomeAssistantEntity From(string entityId, params (string Key, object? Value)[] attributes)
    {
        var domain = entityId.Split('.')[0];
        var entity = domain switch
        {
            "light" => Light(entityId: entityId),
            "switch" => Switch(entityId: entityId),
            "cover" => Cover(entityId: entityId),
            "fan" => Fan(entityId: entityId),
            "climate" => Climate(entityId: entityId),
            "media_player" => MediaPlayer(entityId: entityId),
            "sensor" => Sensor(entityId: entityId),
            "lock" => Lock(entityId: entityId),
            "scene" => Scene(entityId: entityId),
            "script" => Script(entityId: entityId),
            _ => Entity(entityId, "on"),
        };
        entity.Attributes ??= new();
        foreach (var (key, value) in attributes)
        {
            if (value is null)
                continue;
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value));
            entity.Attributes[key] = doc.RootElement.Clone();
        }
        return entity;
    }

    /// <summary>
    /// Low-level entity factory used by every per-domain helper above. Constructs
    /// the entity with the supplied id, state, and JSON-element-wrapped
    /// attributes.
    /// </summary>
    /// <remarks>
    /// Tuples whose value is <c>null</c> are skipped so callers can pass optional
    /// attributes without re-implementing the null check — matching the
    /// production behaviour where HA omits absent attributes rather than emitting
    /// nulls.
    /// </remarks>
    /// <param name="entityId">The entity id to assign.</param>
    /// <param name="state">The HA state string.</param>
    /// <param name="attributes">The attribute (key, value) pairs to attach.</param>
    /// <returns>The fabricated entity.</returns>
    private static HomeAssistantEntity Entity(string entityId, string state, params (string Key, object? Value)[] attributes)
        => new()
        {
            EntityId = entityId,
            State = state,
            Attributes = MakeAttrs(attributes)
        };

    /// <summary>
    /// Wraps each supplied value in a <see cref="JsonElement"/> so the attribute
    /// reader exercises the same coercion paths it does in production.
    /// </summary>
    /// <remarks>
    /// Nulls are filtered out before the round-trip; an absent key in the
    /// resulting dictionary is equivalent to HA omitting the attribute, which the
    /// reader handles by returning a null/default sentinel.
    /// </remarks>
    /// <param name="pairs">The (key, value) pairs to include.</param>
    /// <returns>The attribute dictionary ready to attach to an entity.</returns>
    private static Dictionary<string, object> MakeAttrs(params (string Key, object? Value)[] pairs)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (key, value) in pairs)
        {
            if (value is null)
                continue;
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value));
            dict[key] = doc.RootElement.Clone();
        }
        return dict;
    }
}
