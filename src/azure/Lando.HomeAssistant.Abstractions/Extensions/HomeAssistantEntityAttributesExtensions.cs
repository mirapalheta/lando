using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lando.HomeAssistant.Models;

/// <summary>
/// Strongly typed accessors for the loose <c>attributes</c> bag attached to every
/// Home Assistant entity. HA serializes attribute values as native JSON, so once the
/// payload deserializes into <c>Dictionary&lt;string, object&gt;</c>, every value is
/// actually a <see cref="JsonElement"/>; these helpers paper over that.
/// </summary>
/// <remarks>
/// Lives in the Abstractions project so both the HA-side discovery service and the
/// Alexa-side state translators can share one implementation. All methods are null-safe
/// — they return null/empty rather than throwing when an attribute is absent or the
/// underlying JSON value can't be coerced.
/// </remarks>
public static class HomeAssistantEntityAttributesExtensions
{
    extension(IDictionary<string, object>? attrs)
    {
        /// <summary>
        /// Read a boolean attribute. Returns null on miss or unparseable value.
        /// </summary>
        public bool? GetBool(string key)
        {
            if (attrs?.TryGetValue(key, out var value) != true)
                return null;

            return value switch
            {
                bool b => b,
                JsonElement e => e.GetBoolValue(),
                string s when bool.TryParse(s, out var b) => b,
                1 or 1.0 => true,
                0 or 0.0 => false,
                _ => null
            };
        }

        /// <summary>
        /// Read a string attribute, coercing other primitive shapes when present.
        /// </summary>
        public string? GetString(string key)
        {
            if (attrs?.TryGetValue(key, out var value) != true || value is null)
                return null;

            return value switch
            {
                string s => s,
                JsonElement e => e.GetStringValue(),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Read an int attribute. Returns null on miss or unparseable value.
        /// </summary>
        public int? GetInt(string key)
        {
            if (attrs?.TryGetValue(key, out var value) != true)
                return null;
            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                JsonElement e => e.GetIntValue(),
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }

        /// <summary>
        /// Read a double attribute. Returns null on miss or unparseable value.
        /// </summary>
        public double? GetDouble(string key)
        {
            if (attrs?.TryGetValue(key, out var value) != true)
                return null;
            return value switch
            {
                double d => d,
                int i => i,
                long l => l,
                JsonElement e => e.GetDoubleValue(),
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }

        /// <summary>
        /// Read an attribute that's expected to be a JSON array of strings (or coerce
        /// numeric arrays via <see cref="object.ToString"/>). Returns null when absent.
        /// </summary>
        public IReadOnlyList<string>? GetStringArray(string key)
        {
            if (attrs?.TryGetValue(key, out var value) != true)
                return null;
            if (value is JsonElement e && e.ValueKind == JsonValueKind.Array)
                return e.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.ToString())
                    .ToArray();
            if (value is IEnumerable<object> seq)
                return seq.Select(x => x?.ToString() ?? "").ToArray();
            return null;
        }
    }

    extension(JsonElement element)
    {
        private bool? GetBoolValue()
            => element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(element.GetString(), out var b) => b,
                JsonValueKind.Number when element.TryGetInt32(out var n) => n != 0,
                JsonValueKind.Null => null,
                _ => null
            };

        private string? GetStringValue()
            => element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Null => null,
                _ => element.ToString()
            };

        private int? GetIntValue()
            => element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
                _ => null
            };

        private double? GetDoubleValue()
            => element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetDouble(out var d) => d,
                JsonValueKind.String when double.TryParse(element.GetString(), out var parsed) => parsed,
                _ => null
            };
    }
}
