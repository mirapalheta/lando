using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lando.FunctionApp.Converters;

public sealed class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    // Do NOT let .NET use default string formatting or .ToString("o")
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
}
