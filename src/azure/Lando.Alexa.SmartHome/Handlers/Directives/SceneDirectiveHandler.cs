using System;
using System.Text.Json;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.ChangeReport;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.SceneController;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

/// <summary>
/// Directive handler for the two <c>Alexa.SceneController</c> directives,
/// <c>Activate</c> and <c>Deactivate</c>. Reuses the
/// <see cref="ControlDirectiveHandler{TRequest,TResponse}"/> dispatch flow —
/// resolve entity, run the keyed payload transform, call the HA service, wrap
/// errors — and only overrides the response shape: SceneController replies in
/// its own namespace with <c>ActivationStarted</c> / <c>DeactivationStarted</c>
/// and a <see cref="SceneActivationPayload"/> (cause + timestamp) rather than the
/// generic <c>Alexa.Response</c> + context properties.
/// </summary>
/// <remarks>
/// A single concrete handler serves both directives and both domains. The
/// directive name and event name are supplied at registration
/// (see <c>AddSceneDirectiveHandler</c>), and the HA service call comes from the
/// existing <c>TurnOn</c>/<c>TurnOff</c> payload transform keyed under that
/// directive (<c>turn_on</c> for Activate, <c>turn_off</c> for Deactivate). The
/// target domain (<c>scene</c> vs <c>script</c>) is read from the entity id, so
/// scenes and scripts share this handler.
/// </remarks>
internal sealed class SceneDirectiveHandler(
    IServiceProvider provider,
    string directiveName,
    string eventName,
    IValidator<EmptyPayload> validator,
    IOptions<JsonSerializerOptions> jsonOptions,
    ILogger<SceneDirectiveHandler> logger)
    : ControlDirectiveHandler<EmptyPayload, SceneActivationPayload>(provider, directiveName, validator, jsonOptions, logger)
{
    /// <inheritdoc />
    protected override string Namespace => Namespaces.SceneController;

    /// <inheritdoc />
    protected override string EventName => eventName;

    /// <inheritdoc />
    protected override SceneActivationPayload CreateResponse() => new()
    {
        Cause = new() { Type = ChangeCauseType.VoiceInteraction },
        Timestamp = DateTime.UtcNow
    };
}
