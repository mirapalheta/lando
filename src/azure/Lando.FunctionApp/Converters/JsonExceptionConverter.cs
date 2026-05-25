using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lando.FunctionApp.Converters;

public sealed class JsonExceptionConverter : JsonConverter<Exception>
{
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();

        WriteException(writer, value, options);
    }

    private void WriteException(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteString(ConvertName(options, nameof(Exception.Message)), value.Message);

        if (value.InnerException is not null)
        {
            writer.WriteStartObject(ConvertName(options, nameof(Exception.InnerException)));
            WriteException(writer, value.InnerException, options);
        }

        if (value?.StackTrace is not null)
        {
            writer.WriteString(ConvertName(options, nameof(Exception.StackTrace)), value.StackTrace);
        }

        writer.WriteString(ConvertName(options, nameof(Type)), value?.GetType().ToString());
        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ConvertName(JsonSerializerOptions options, string name)
        => options.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
