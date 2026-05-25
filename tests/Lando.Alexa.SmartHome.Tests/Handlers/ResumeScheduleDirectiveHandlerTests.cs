using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.Alexa.SmartHome.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="ResumeScheduleDirectiveHandler"/> is intentionally a no-op
/// acknowledgement (HA has no generic resume-schedule service). Test pins
/// the response shape so a future contributor doesn't introduce side effects.
/// </summary>
public class ResumeScheduleDirectiveHandlerTests
{
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Acknowledges_directive_with_empty_Response()
    {
        var sut = new ResumeScheduleDirectiveHandler(
            validator: null!, JsonOptions,
            NullLogger<ResumeScheduleDirectiveHandler>.Instance);
        var request = RequestFixtures.Directive(
            Namespaces.ThermostatController, DirectiveNames.ResumeSchedule,
            payload: new ResumeSchedulePayload(),
            endpoint: RequestFixtures.Endpoint(endpointId: "climate#living_room"));

        var response = await sut.HandleAsync(request, CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.Alexa);
        response.Event.Header.Name.ShouldBe(EventNames.Response);
        response.Event.Payload.ShouldBeOfType<EmptyPayload>();
    }
}
