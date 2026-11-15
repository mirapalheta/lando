using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.ChangeReport;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Lando.Alexa.Constants.HttpClients;

namespace Lando.Alexa.SmartHome.Services;

/// <summary>
/// Sends proactive <c>Alexa.ChangeReport</c> events to the Alexa Event Gateway.
/// </summary>
/// <remarks>
/// <para>
/// The HTTP client's base address is <c>https://api.amazonalexa.com/</c>; all requests
/// target the <c>v3/events</c> path. Any 2xx response is a success.
/// </para>
/// <para>
/// Each user's event is sent up to <see cref="AlexaApi.MaxAttempts"/> times. An attempt
/// that times out, fails in transport, or gets 429 or 5xx is retried after
/// <see cref="RetryDelay"/>; any other status is final. Every attempt carries the same
/// serialized event, so the gateway sees one <c>messageId</c> however many times it is sent.
/// </para>
/// </remarks>
public sealed class EventGatewayClient(HttpClient client, ITokenStore tokenStore, IOptions<JsonSerializerOptions> jsonOptions, ILogger<EventGatewayClient> logger) : IEventGatewayClient
{
    private const string EventsPath = "v3/events";
    private const string TimeoutStatus = "timeout";

    /// <summary>
    /// The timeout for one attempt. Shorter than the client's own <see cref="HttpClient.Timeout"/>,
    /// which applies to each request and so never limits the sequence. Settable for tests.
    /// </summary>
    internal TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(AlexaApi.AttemptTimeoutSeconds);

    /// <summary>
    /// The pause between two attempts. Settable for tests.
    /// </summary>
    internal TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(AlexaApi.RetryDelaySeconds);

    /// <inheritdoc />
    public async ValueTask<bool> SendChangeReportAsync(string endpointId, ContextProperty[] changed, ContextProperty[] all, CancellationToken cancellationToken)
    {
        var tokens = await tokenStore.ListAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Sending ChangeReport for '{EndpointId}' to {UserCount} user(s)", endpointId, tokens.Count());

        var tasks = tokens.Select(token => SendChangeReportAsync(endpointId, token.userId, token.value, changed, all, cancellationToken));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.All(a => a);
    }

    private async Task<bool> SendChangeReportAsync(
        string endpointId,
        string userId,
        string accessToken,
        ContextProperty[] changed,
        ContextProperty[] all,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var attempts = 0;
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

            // Serialized once, so a retry resends the same bytes and the same messageId.
            var body = JsonSerializer.SerializeToUtf8Bytes(content, jsonOptions.Value);

            while (true)
            {
                attempts++;
                var attemptStarted = Stopwatch.GetTimestamp();
                var (succeeded, retryable, status) = await PostAsync(body, accessToken, cancellationToken).ConfigureAwait(false);

                if (succeeded)
                {
                    logger.LogInformation(
                        "ChangeReport for '{EndpointId}' sent: attempts {Attempts}, status {Status}, {ElapsedMs} ms",
                        endpointId, attempts, status, ElapsedMs(started));
                    return true;
                }

                logger.LogWarning(
                    "ChangeReport for '{EndpointId}' attempt {Attempt} of {MaxAttempts} failed for user {UserId}: status {Status}, {ElapsedMs} ms",
                    endpointId, attempts, AlexaApi.MaxAttempts, userId, status, ElapsedMs(attemptStarted));

                if (!retryable || attempts >= AlexaApi.MaxAttempts)
                {
                    logger.LogError(
                        "ChangeReport for '{EndpointId}' lost for user {UserId}: attempts {Attempts}, status {Status}, {ElapsedMs} ms",
                        endpointId, userId, attempts, status, ElapsedMs(started));
                    return false;
                }

                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The host is stopping (or the caller gave up): not a gateway failure, and nothing to retry.
            logger.LogInformation(
                "ChangeReport for '{EndpointId}' cancelled: attempts {Attempts}, {ElapsedMs} ms",
                endpointId, attempts, ElapsedMs(started));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send '{EndpointId}' ChangeReport for user {UserId}", endpointId, userId);
            return false;
        }
    }

    /// <summary>
    /// Makes one attempt and classifies its outcome. Throws only when the caller's token is
    /// cancelled, or on an exception no retry would fix.
    /// </summary>
    private async Task<(bool Succeeded, bool Retryable, string Status)> PostAsync(byte[] body, string accessToken, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AttemptTimeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsPath)
            {
                Content = new ByteArrayContent(body) { Headers = { ContentType = new("application/json") { CharSet = "utf-8" } } },
                Headers = { Authorization = new("Bearer", accessToken) }
            };

            using var response = await client.SendAsync(request, timeout.Token).ConfigureAwait(false);
            var status = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            return (response.IsSuccessStatusCode, retryable, status);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Our per-attempt timeout, or the client's own: either way the gateway did not answer in time.
            return (false, true, TimeoutStatus);
        }
        catch (HttpRequestException ex)
        {
            return (false, true, $"{nameof(HttpRequestException)} ({ex.HttpRequestError}): {ex.Message}");
        }
    }

    private static long ElapsedMs(long startTimestamp) => (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
