using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lando.HomeAssistant.Models;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Strongly typed accessors over the raw <see cref="HomeAssistantEntity"/> shape.
/// Each method papers over Home Assistant's permissive JSON schema (attributes are a
/// loose object bag and may be absent, null, or in the wrong type) and returns a
/// caller-friendly value or a safe default.
/// </summary>
/// <remarks>
/// Centralizing attribute reads here means the per-domain transformers stay focused
/// on their actual job — turning entity state into discovery endpoints and context
/// properties — without each one re-implementing the same defensive parsing pattern.
/// </remarks>
public static class HomeAssistantEntityExtensions
{
    extension(HomeAssistantEntity entity)
    {
        /// <summary>
        /// Determines whether the entity is exposed to an integration by reading a custom attribute
        /// </summary>
        /// <param name="customAttribute">The custom attribute to check for exposure; if null or whitespace, falls back to the default <c>expose</c> attribute.</param>
        /// <returns><c>true</c> if the entity is exposed; otherwise, <c>false</c>.</returns>
        public bool IsExposed(string customAttribute)
            => entity.Attributes.GetBool(customAttribute)
            ?? entity.Attributes.GetBool(CustomAttributes.Expose)
            ?? false;

        /// <summary>
        /// Extracts the HA domain from the entity id by taking the part before the first
        /// <c>'.'</c> (for example <c>"light.living_room"</c> → <c>"light"</c>).
        /// </summary>
        /// <remarks>
        /// Falls back to <see cref="Domains.Unknown"/> when the entity or entity id is
        /// missing — callers can switch on the result without null-checking first.
        /// </remarks>
        /// <returns>
        /// The lower-cased domain string, or <see cref="Domains.Unknown"/> when not
        /// determinable.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetDomain()
            => entity.EntityId.GetDomain() ?? Domains.Unknown;

        /// <summary>
        /// Reads <c>attributes.friendly_name</c>, falling back to the entity id when the
        /// integration did not provide one.
        /// </summary>
        /// <remarks>
        /// HA's <c>friendly_name</c> is what users see in the dashboard; using the entity
        /// id as a fallback is preferable to returning <c>null</c> so the Alexa app
        /// always has something to render.
        /// </remarks>
        /// <param name="customAttribute">
        /// Optional integration-specific attribute checked first (e.g. <c>alexa_name</c>),
        /// before the bridge-wide <c>lando_name</c> / HA <c>friendly_name</c> chain.
        /// </param>
        /// <returns>The friendly name, or the entity id when none is set.</returns>
        public string GetFriendlyName(string? customAttribute = null)
            => (string.IsNullOrWhiteSpace(customAttribute) ? default : entity.Attributes.GetString(customAttribute))
            ?? entity.Attributes.GetString(CustomAttributes.Name)
            ?? entity.Attributes.GetString(EntityAttributes.FriendlyName)
            ?? entity.EntityId
            ?? string.Empty;

        /// <summary>
        /// Reads <c>attributes.device_class</c>, lower-cased to match the canonical HA
        /// values, or returns <c>null</c> when the integration did not provide one.
        /// </summary>
        /// <remarks>
        /// Device class is used to narrow a domain — for example to distinguish a
        /// <c>cover</c> that is a window blind from one that is a garage door — and the
        /// transformers branch on its value when picking display categories and
        /// capabilities.
        /// </remarks>
        /// <returns>The lower-cased device class, or <c>null</c> when not set.</returns>
        public string? GetDeviceClass()
            => entity.Attributes.GetString(EntityAttributes.DeviceClass)?.ToLowerInvariant();

        /// <summary>
        /// Reads <c>attributes.supported_features</c> as a bitmask, or <c>0</c> when the
        /// integration did not provide one.
        /// </summary>
        /// <remarks>
        /// The bit layout depends on the entity's domain — see for example
        /// <see cref="CoverFeatures"/>, <see cref="FanFeatures"/>,
        /// <see cref="ClimateFeatures"/>, and <see cref="MediaPlayerFeatures"/>.
        /// </remarks>
        /// <returns>The bitmask value, or <c>0</c> when not set.</returns>
        public int GetSupportedFeatures()
            => entity.Attributes.GetInt(EntityAttributes.SupportedFeatures) ?? 0;

        /// <summary>
        /// Reads <c>attributes.supported_color_modes</c> on a light, or an empty list when
        /// the light entity does not expose one.
        /// </summary>
        /// <remarks>
        /// The modern HA <c>supported_color_modes</c> attribute supersedes the legacy
        /// <c>SUPPORT_*</c> bits on the <c>supported_features</c> bitmask for the
        /// brightness / color capabilities, so consumers should branch on this instead
        /// of the bitmask when deciding which controllers to advertise.
        /// </remarks>
        /// <returns>
        /// The list of supported color modes, or an empty list when none is present.
        /// </returns>
        public IReadOnlyList<string> GetSupportedColorModes()
            => entity.Attributes.GetStringArray(EntityAttributes.SupportedColorModes) ?? [];

        /// <summary>
        /// Reads <c>attributes.unit_of_measurement</c>, used by the climate transformer
        /// to decide whether to emit temperatures in Celsius or Fahrenheit.
        /// </summary>
        /// <remarks>
        /// HA stores the unit as the literal symbol (for example <c>"°F"</c>), not as an
        /// ISO scale name; callers usually need to inspect the string for an <c>F</c> or
        /// <c>C</c> rather than parsing it as an enum.
        /// </remarks>
        /// <returns>The unit string, or <c>null</c> when not present.</returns>
        public string? GetUnitOfMeasurement()
            => entity.Attributes.GetString(EntityAttributes.UnitOfMeasurement);

        /// <summary>
        /// Reads <c>attributes.hvac_modes</c> from a climate entity — the list of modes
        /// that entity can be switched into.
        /// </summary>
        /// <remarks>
        /// Used by the climate transformer when surfacing thermostat capabilities to
        /// Alexa. Returns an empty list when the entity does not advertise modes (rare
        /// in practice for climate entities but tolerated).
        /// </remarks>
        /// <returns>
        /// The supported HVAC modes, or an empty list when no modes are advertised.
        /// </returns>
        public IReadOnlyList<string> GetHvacModes()
            => entity.Attributes.GetStringArray(EntityAttributes.HvacModes) ?? [];
    }

    /// <summary>
    /// Extracts the HA domain from the entity id carried on an outbound service-call
    /// request, so the service caller can build the right service URL.
    /// </summary>
    /// <remarks>
    /// Identical semantics to <see cref="GetDomain(HomeAssistantEntity?)"/>; this
    /// overload exists so request-side code doesn't need to materialize the entity
    /// just to learn the domain.
    /// </remarks>
    /// <param name="request">The request whose entity id to inspect.</param>
    /// <returns>
    /// The lower-cased domain string, or <see cref="Domains.Unknown"/> when not
    /// determinable.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDomain(this HomeAssistantRequest? request)
        => request?.EntityId.GetDomain() ?? Domains.Unknown;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetDomain(this string? entityId)
        => string.IsNullOrWhiteSpace(entityId) ? Domains.Unknown : entityId.Split('.')[0].ToLowerInvariant();
}
