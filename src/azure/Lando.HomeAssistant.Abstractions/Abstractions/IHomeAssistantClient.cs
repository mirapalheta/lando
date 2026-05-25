namespace Lando.HomeAssistant;

/// <summary>
/// Unified Home Assistant client surface that combines discovery and service-call
/// responsibilities. Composing them on a single interface lets callers depend on one
/// abstraction when they need both — for example a directive handler that reads
/// current state and then dispatches a service call.
/// </summary>
/// <remarks>
/// Callers that only need one of the two responsibilities should prefer the narrower
/// <see cref="IDeviceDiscovery"/> or <see cref="IServiceCaller"/> dependency — both
/// stay registered alongside the unified client in DI.
/// </remarks>
public interface IHomeAssistantClient : IDeviceDiscovery, IServiceCaller
{
}
