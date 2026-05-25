using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.HomeAssistant.Services;

/// <summary>
/// HTTP-backed implementation of <see cref="IServiceCaller"/>. Posts a
/// <see cref="HomeAssistantRequest"/> to <c>services/&lt;domain&gt;/&lt;service&gt;</c>
/// on the configured Home Assistant instance and translates transport or
/// HTTP failures into <see cref="HomeAssistantException"/>.
/// </summary>
/// <remarks>
/// The keyed <see cref="HttpClient"/> is provisioned with the bridge's HA
/// base URL, long-lived bearer token, optional Tailscale/SOCKS proxy, and any
/// custom CA — all routing concerns live in the HTTP layer, not here.
/// </remarks>
public class ServiceCallerService([FromKeyedServices(Constants.HomeAssistant)] HttpClient client, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ServiceCallerService> logger) : IServiceCaller
{
    /// <inheritdoc />
    public async Task CallServiceAsync(HomeAssistantRequest data, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var domain = data.GetDomain();
            var url = $"services/{domain}/{data.Service}";
            var payload = JsonSerializer.Serialize(data, jsonOptions.Value);

            logger.LogInformation("Calling Home Assistant service {Domain}.{Service} for {EntityId} with payload: {Payload}", domain, data.Service, data.EntityId, payload);

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogError("Service call failed: {Status} - {Error} after {ElapsedMs}ms", response.StatusCode, errorBody, sw.ElapsedMilliseconds);
                throw new HomeAssistantException($"Error calling service {data.Service} on {data.EntityId}: {response.StatusCode} - {errorBody}");
            }

            logger.LogInformation("Service call succeeded: {Domain}.{Service} on {EntityId} in {ElapsedMs}ms", domain, data.Service, data.EntityId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not HomeAssistantException)
        {
            logger.LogError(
                ex, "Error calling service {Service} on {EntityId} after {ElapsedMs}ms",
                data.Service, data.EntityId, sw.ElapsedMilliseconds
            );

            throw new HomeAssistantException($"Error calling service {data.Service} on {data.EntityId}: {ex.Message}", ex);
        }
    }
}
