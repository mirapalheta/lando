using System;
using System.Text.Json;

namespace Lando.FunctionApp.Converters.Tests;

public class JsonExceptionConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonExceptionConverter() }
    };

    [Fact]
    public void Write_ShouldWriteExceptionToJson()
    {
        // Arrange
        Exception exception;
        try
        {
            throw new Exception("Test Exception", new Exception("Inner Exception"));
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var json = JsonSerializer.Serialize(exception, _options);

        // Assert
        json.ShouldNotBeNullOrEmpty();
        json.ShouldMatch("^{\"Message\"\\:\"Test Exception\"\\,\"InnerException\":{\"Message\"\\:\"Inner Exception\"\\,\"Type\"\\:\"System\\.Exception\"}\\,\"StackTrace\"\\:\".*\"\\,\"Type\"\\:\"System.Exception\"}$");
    }

    [Fact]
    public void Read_ShouldThrowNotSupportedException()
    {
        // Arrange
        var act = () => JsonSerializer.Deserialize<Exception>("{}", _options);

        // Act & Assert
        act.ShouldThrow<NotSupportedException>();
    }
}
