using System;
using System.Text.Json;

namespace Lando.FunctionApp.Converters.Tests;

public class JsonDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonDateTimeConverter() }
    };

    [Fact]
    public void Write_ShouldWriteDateTimeToJson()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 6, 12, 0, 0);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        json.ShouldNotBeNullOrEmpty();
        json.ShouldBe("\"2024-06-06T12:00:00.000Z\"");
    }

    [Fact]
    public void Read_ShouldThrowNotSupportedException()
    {
        // Arrange
        var json = "\"2024-06-06T12:00:00\"";

        // Act
        var value = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Act & Assert
        value.ShouldBe(new DateTime(2024, 6, 6, 12, 0, 0));
    }
}
