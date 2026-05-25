using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.Security.LWA;
using Lando.Alexa.SmartHome.Models.Authorization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

using static Constants;

/// <summary>
/// Handles <c>Alexa.Authorization.AcceptGrant</c> — the one-time hand-off Alexa
/// sends when a customer first enables the skill.
/// </summary>
/// <remarks>
/// <para>
/// Three steps:
/// </para>
/// <list type="number">
///   <item>Resolve the grantee's LWA <c>user_id</c> by introspecting
///         <c>payload.grantee.token</c>. This keys the refresh-token store
///         so the bridge can later mint per-customer access tokens for the
///         Alexa Event Gateway.</item>
///   <item>Exchange <c>payload.grant.code</c> at
///         <c>https://api.amazon.com/auth/o2/token</c> for a refresh + access
///         token pair (<see cref="LwaTokenClient.ExchangeCodeAsync"/>).</item>
///   <item>Persist the refresh token via <see cref="ITokenStore"/>.</item>
/// </list>
/// <para>
/// Any failure becomes an <see cref="ErrorType.AcceptGrantFailed"/>
/// error response per Alexa's documented contract — re-enabling the skill
/// from the Alexa app retries the whole flow.
/// </para>
/// </remarks>
internal class AcceptGrantDirectiveHandler(IValidator<AcceptGrantPayload> validator, IOptions<JsonSerializerOptions> jsonOptions,
        [FromKeyedServices(TokenStore)] ITokenStore store, ILogger<AcceptGrantDirectiveHandler> logger
    ) : DirectiveHandler<AcceptGrantPayload, EmptyPayload>(validator, jsonOptions, logger)
{
    /// <inheritdoc />
    public override string DirectiveName => DirectiveNames.AcceptGrant;

    /// <inheritdoc />
    protected override string Namespace => Namespaces.Authorization;

    /// <inheritdoc />
    protected override string EventName => EventNames.AcceptGrantResponse;

    /// <inheritdoc />
    protected override async Task<(EmptyPayload, ContextProperty[]?)> HandleAsync(string? _, AcceptGrantPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: resolve the grantee user_id from the bearer token Alexa attached.
            var granteeInfo = await store.Client.GetAsync(payload.Grantee.Token.Value!, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(granteeInfo?.UserId))
            {
                Logger.LogWarning("AcceptGrant: could not resolve grantee user_id from grantee.token");
                throw new AlexaSmartHomeException(ErrorType.AcceptGrantFailed, "Unable to resolve grantee identity");
            }

            // Step 2: exchange the one-time code for a refresh+access token pair.
            var minted = await store.Client.ExchangeCodeAsync(payload.Grant.Code, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(minted.RefreshToken))
            {
                Logger.LogWarning("AcceptGrant: LWA returned access_token without refresh_token; cannot persist long-lived grant");
                throw new AlexaSmartHomeException(ErrorType.AcceptGrantFailed, "LWA token response missing refresh_token");
            }

            // Step 3: persist the refresh token so the bridge can later post to the Event Gateway.
            await store.SaveAsync(granteeInfo.UserId, minted.RefreshToken, cancellationToken).ConfigureAwait(false);

            Logger.LogInformation("AcceptGrant succeeded for grantee");
            return (EmptyPayload.Instance, default);
        }
        catch (LwaTokenException ex)
        {
            Logger.LogWarning(ex, "AcceptGrant: LWA token exchange failed");
            throw new AlexaSmartHomeException(ErrorType.AcceptGrantFailed, "Failed to exchange authorization code", ex);
        }
        catch (Exception ex) when (ex is not AlexaSmartHomeException)
        {
            Logger.LogError(ex, "AcceptGrant: unexpected failure persisting grant");
            throw new AlexaSmartHomeException(ErrorType.AcceptGrantFailed, "Failed to persist authorization grant", ex);
        }
    }
}
