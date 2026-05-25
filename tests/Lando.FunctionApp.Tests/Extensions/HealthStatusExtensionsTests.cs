using System;
using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lando.FunctionApp.Extensions.Tests;

/// <summary>
/// <see cref="HealthStatusExtensions.ToHttpStatusCode"/> is the only mapping
/// between the .NET health-check verdicts and the HTTP status the function
/// returns. Keeping Degraded at 200 keeps the container in load-balancer
/// rotation while still signalling the underlying issue in the JSON body.
/// </summary>
public class HealthStatusExtensionsTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, HttpStatusCode.OK)]
    [InlineData(HealthStatus.Degraded, HttpStatusCode.OK)]
    [InlineData(HealthStatus.Unhealthy, HttpStatusCode.ServiceUnavailable)]
    public void Maps_known_statuses(HealthStatus status, HttpStatusCode expected)
        => status.ToHttpStatusCode().ShouldBe(expected);

    [Fact]
    public void Throws_NotSupportedException_for_unknown_status()
        => Should.Throw<NotSupportedException>(() => ((HealthStatus)99).ToHttpStatusCode());
}
