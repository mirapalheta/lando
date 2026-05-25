using System.Text.Json;

namespace Lando.FunctionApp.Converters.Tests;

public class JsonDoubleConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonDoubleConverter() }
    };

    [Fact]
    public void Write_ShouldWriteDoubleToJson()
    {
        // Arrange
        double value = 123.45;

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.ShouldNotBeNullOrEmpty();
        json.ShouldBe("123.45");
    }

    [Fact]
    public void Read_ShouldReadDoubleFromJson()
    {
        // Arrange
        var json = "123.45";

        // Act
        var value = JsonSerializer.Deserialize<double>(json, _options);

        // Assert
        value.ShouldBe(123.45);
    }
}
