using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lando.HomeAssistant.Services;

/// <summary>
/// HTTP-backed implementation of <see cref="IDeviceDiscovery"/>. Fetches entities from
/// the Home Assistant REST API and yields them as an async stream. Transport and HTTP
/// errors are translated to <see cref="HomeAssistantException"/> so callers can handle
/// a single domain failure type.
/// </summary>
public class DeviceDiscoveryService(
    [FromKeyedServices(Constants.HomeAssistant)] HttpClient client,
    ILogger<DeviceDiscoveryService> logger) : IDeviceDiscovery
{
    /// <inheritdoc />
    public async IAsyncEnumerable<HomeAssistantEntity> ListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var count = 0;
        var skipped = 0;

        Stream stream = await OpenListStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream)
        {
            await foreach (var entity in JsonSerializer
                               .DeserializeAsyncEnumerable<HomeAssistantEntity>(stream, cancellationToken: cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (entity is null)
                {
                    // HA occasionally returns sparse arrays during startup; one bad element
                    // shouldn't drop the whole discovery pass.
                    skipped++;
                    continue;
                }

                count++;
                yield return entity;
            }
        }

        if (skipped > 0)
            logger.LogWarning("Skipped {SkippedCount} null entities while listing HA states", skipped);

        logger.LogInformation("Discovered {DeviceCount} entities in {ElapsedMs}ms", count, sw.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public async Task<HomeAssistantEntity?> GetAsync(string entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Entity ID must be provided", nameof(entityId));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"states/{entityId}");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogError("Getting HA entity {EntityId} failed: {Status} - {Error}", entityId, response.StatusCode, errorBody);
                throw new HomeAssistantException($"Getting Home Assistant entity '{entityId}' failed: {response.StatusCode} - {errorBody}");
            }

            return await response.Content
                .ReadFromJsonAsync<HomeAssistantEntity>(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HomeAssistantException and not ArgumentException and not OperationCanceledException)
        {
            logger.LogError(ex, "Transport error while getting HA entity {EntityId}", entityId);
            throw new HomeAssistantException($"Transport error while getting Home Assistant entity '{entityId}'", ex);
        }
    }

    private async Task<Stream> OpenListStreamAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "states");
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                response.Dispose();
                logger.LogError("Listing HA entities failed: {Status} - {Error}", response.StatusCode, errorBody);
                throw new HomeAssistantException($"Listing Home Assistant entities failed: {response.StatusCode} - {errorBody}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HomeAssistantException and not OperationCanceledException)
        {
            logger.LogError(ex, "Transport error while listing HA entities");
            throw new HomeAssistantException("Transport error while listing Home Assistant entities", ex);
        }
    }
}
