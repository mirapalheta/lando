using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.FunctionApp.Functions;

/// <summary>
/// Azure Functions HTTP-trigger for the <c>GET /health</c> endpoint. Runs the
/// registered <see cref="HealthCheckService"/> and serialises the resulting
/// <see cref="HealthReport"/> to JSON. The HTTP status code is derived from
/// the aggregate status (see <see cref="HealthStatusExtensions.ToHttpStatusCode"/>).
/// </summary>
public class HealthCheck
{
    /// <summary>
    /// The Functions runtime entry point for the health endpoint.
    /// </summary>
    /// <param name="req">The inbound HTTP request.</param>
    /// <param name="context">The Functions invocation context.</param>
    /// <param name="cancellationToken">Cancellation token tied to the invocation lifetime.</param>
    [Function("HealthCheck")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken = default)
    {
        var logger = context.GetLogger<HealthCheck>();

        logger.LogInformation("Health check requested");

        var service = context.InstanceServices.GetRequiredService<HealthCheckService>();
        var options = context.InstanceServices.GetRequiredService<IOptions<JsonSerializerOptions>>();
        var report = await service.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        var response = req.CreateResponse(report.Status.ToHttpStatusCode());
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await JsonSerializer.SerializeAsync(response.Body, report, options.Value, cancellationToken).ConfigureAwait(false);
        return response;
    }
}
