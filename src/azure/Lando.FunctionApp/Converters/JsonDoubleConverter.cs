using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lando.FunctionApp.Converters;

public sealed class JsonDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDouble();

    // "0.0########" guarantees at least 1 decimal place, and up to 8 if necessary
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        => writer.WriteRawValue(value.ToString("0.0########", CultureInfo.InvariantCulture));
}
