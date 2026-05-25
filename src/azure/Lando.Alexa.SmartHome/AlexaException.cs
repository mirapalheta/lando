using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;

namespace Lando.Alexa.SmartHome;

internal class AlexaSmartHomeException : Exception
{
    public AlexaSmartHomeException(ErrorType error, string message) : base(message)
        => Error = error;

    public AlexaSmartHomeException(ErrorType error, string message, Exception innerException) : base(message, innerException)
        => Error = error;

    public ErrorType Error { get; private set; }
}
