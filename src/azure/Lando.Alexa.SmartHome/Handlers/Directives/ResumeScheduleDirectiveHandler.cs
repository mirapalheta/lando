using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

/// <summary>
/// Handles <c>Alexa.ThermostatController.ResumeSchedule</c>. HA doesn't have
/// a generic "resume schedule" service — schedules are a per-integration
/// concept (Nest, Ecobee, etc.) and aren't surfaced through the climate
/// domain — so the handler acknowledges the directive without acting on it,
/// preventing Alexa from looping on retries.
/// </summary>
/// <remarks>
/// If a specific thermostat integration exposes a resume-schedule service
/// (for example <c>nest.set_mode: schedule</c>), replace this no-op with
/// a domain-specific service call.
/// </remarks>
internal class ResumeScheduleDirectiveHandler(IValidator<ResumeSchedulePayload> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ResumeScheduleDirectiveHandler> logger)
    : DirectiveHandler<ResumeSchedulePayload, EmptyPayload>(validator, jsonOptions, logger)
{
    /// <inheritdoc />
    public override string DirectiveName => DirectiveNames.ResumeSchedule;

    /// <inheritdoc />
    protected override async Task<(EmptyPayload, ContextProperty[]?)> HandleAsync(string? _, ResumeSchedulePayload payload, CancellationToken cancellationToken)
        => (EmptyPayload.Instance, default);
}
