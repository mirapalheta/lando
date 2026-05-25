using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Lando.HomeAssistant.HealthChecks;

/// <summary>
/// Health check that issues a GET against the configured HA REST API base
/// (<c>/api/</c>) using the same keyed <see cref="HttpClient"/> the rest of
/// the bridge uses — so any breakage in the auth header, TLS pinning, or
/// proxy routing surfaces here before it surfaces in a directive handler.
/// </summary>
/// <remarks>
/// Reports <see cref="HealthStatus.Healthy"/> when HA returns a 2xx response.
/// Returns <see cref="HealthStatus.Unhealthy"/> with a descriptive message for
/// transport failures, timeouts, and unexpected exceptions, attaching the HA
/// base URL to the result data for quick diagnostics.
/// </remarks>
public class HomeAssistantHealthCheck(IServiceProvider provider) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var logger = default(ILogger<HomeAssistantHealthCheck>?);

        try
        {
            logger = provider.GetService<ILogger<HomeAssistantHealthCheck>>();
            var client = provider.GetRequiredKeyedService<HttpClient>(Constants.HomeAssistant);
            data["host"] = client.BaseAddress?.ToString() ?? "UNDEFINED";

            // Test connectivity by discovering devices
            using var response = await client.GetAsync("", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            logger?.LogInformation("Home Assistant health check passed");
            return HealthCheckResult.Healthy("Home Assistant is reachable", data);
        }
        catch (HttpRequestException ex)
        {
            logger?.LogError(ex, "Home Assistant health check failed - connection error");
            return HealthCheckResult.Unhealthy("Cannot reach Home Assistant", ex, data);
        }
        catch (TaskCanceledException ex)
        {
            logger?.LogError(ex, "Home Assistant health check timed out");
            return HealthCheckResult.Unhealthy("Home Assistant request timed out", ex, data);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Home Assistant health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Unexpected error checking Home Assistant health", ex, data);
        }
    }
}
