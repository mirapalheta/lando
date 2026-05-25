using System;
using System.Collections.Generic;
using System.Net.Http;
using HealthChecks.Uris;
using Lando.HomeAssistant.Configuration;
using Lando.HomeAssistant.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods that register the bridge's Home Assistant health checks
/// (a proxy reachability probe and the
/// <see cref="HomeAssistantHealthCheck"/>) on
/// an <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class IHealthChecksBuilderExtensions
{
    private const string NAME = "home-assistant";

    /// <summary>
    /// Add a health check for Home Assistant.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'home-assistant' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddHomeAssistant(
        this IHealthChecksBuilder builder,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default)
    {
        builder.Services.AddSingleton<HomeAssistantHealthCheck>();

        name ??= NAME;

        return builder.Add(new HealthCheckRegistration(
            $"{name}-proxy",
            provider =>
            {
                var config = provider.GetRequiredService<HomeAssistantClientOptions>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

                var options = new UriHealthCheckOptions();
                if (!string.IsNullOrWhiteSpace(config.ProxyHealthCheckUrl))
                {
                    options.AddUri(new(config.ProxyHealthCheckUrl), null);
                }

                return new UriHealthCheck(
                    options,
                    () => httpClientFactory.CreateClient($"{name}-proxy")
                );
            },
            failureStatus,
            tags,
            timeout
        )).Add(new HealthCheckRegistration(
            $"{name}-client",
            sp => sp.GetRequiredService<HomeAssistantHealthCheck>(),
            failureStatus, tags, timeout
        ));
    }
}
