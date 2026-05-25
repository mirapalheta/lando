using System;
using System.Net;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Maps a <see cref="HealthStatus"/> to the HTTP status the health endpoint
/// should return. <c>Healthy</c> and <c>Degraded</c> both surface as 200 OK —
/// degraded keeps the container in rotation while signalling the issue in
/// the JSON body — and <c>Unhealthy</c> surfaces as 503 so the platform's
/// load-balancer will drain traffic.
/// </summary>
public static class HealthStatusExtensions
{
    /// <summary>
    /// Returns the HTTP status code corresponding to <paramref name="status"/>.
    /// </summary>
    /// <param name="status">The aggregated status from the health-check service.</param>
    /// <returns>
    /// <see cref="HttpStatusCode.OK"/> for healthy/degraded;
    /// <see cref="HttpStatusCode.ServiceUnavailable"/> for unhealthy.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if a future <see cref="HealthStatus"/> value isn't mapped — the
    /// throw is a deliberate signal to revisit this method rather than emit
    /// a misleading 200.
    /// </exception>
    public static HttpStatusCode ToHttpStatusCode(this HealthStatus status)
        => status switch
        {
            HealthStatus.Healthy or HealthStatus.Degraded => HttpStatusCode.OK,
            HealthStatus.Unhealthy => HttpStatusCode.ServiceUnavailable,
            _ => throw new NotSupportedException()
        };
}
