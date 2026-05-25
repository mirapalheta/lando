using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.ChangeReport;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Services;

/// <summary>
/// Sends proactive <c>Alexa.ChangeReport</c> events to the Alexa Event Gateway.
/// </summary>
/// <remarks>
/// The HTTP client's base address is <c>https://api.amazonalexa.com/</c>; all requests
/// target the <c>v3/events</c> path. A 202 Accepted response is the only success code.
/// </remarks>
public sealed class EventGatewayClient(HttpClient client, ITokenStore tokenStore, IOptions<JsonSerializerOptions> jsonOptions, ILogger<EventGatewayClient> logger) : IEventGatewayClient
{
    private const string EventsPath = "v3/events";

    /// <inheritdoc />
    public async ValueTask<bool> SendChangeReportAsync(string endpointId, ContextProperty[] changed, ContextProperty[] all, CancellationToken cancellationToken)
    {
        var tokens = await tokenStore.ListAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Sending ChangeReport for '{EndpointId}' to {UserCount} user(s)", endpointId, tokens.Count());

        var tasks = tokens.Select(token => SendChangeReportAsync(endpointId, token.userId, token.value, changed, all, cancellationToken));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.All(a => a);
    }

    /// <inheritdoc />
    private async Task<bool> SendChangeReportAsync(
        string endpointId,
        string userId,
        string accessToken,
        ContextProperty[] changed,
        ContextProperty[] all,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new Response
            {
                Event = Event.Create(
                    Namespaces.Alexa, EventNames.ChangeReport,
                    new ChangeReportPayload
                    {
                        Change = new()
                        {
                            Cause = new() { Type = ChangeCauseType.PhysicalInteraction },
                            Properties = [.. changed]
                        }
                    },
                    new()
                    {
                        Scope = new Scope { Type = ScopeType.BearerToken, Token = new(accessToken) },
                        EndpointId = endpointId
                    }
                ),
                Context = new() { Properties = [.. all] }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, EventsPath)
            {
                Content = JsonContent.Create(content, options: jsonOptions.Value),
                Headers = { Authorization = new("Bearer", accessToken) }
            };

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send '{EndpointId}' ChangeReport for user {UserId}", endpointId, userId);
            return false;
        }
    }
}
