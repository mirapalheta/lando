using Lando.Alexa.SmartHome.Models.ErrorResponse;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Fluent factories for building <see cref="Response"/> objects directly from an inbound
/// <see cref="Request"/>. Centralises the rules for which Alexa events include a
/// <c>context</c> block, which include an <c>endpoint</c>, and which require a
/// correlation token to be echoed back.
/// </summary>
internal static class RequestExtensions
{
    extension(Request request)
    {
        /// <summary>
        /// Build a generic <c>Alexa.Response</c> for a successful device-targeted directive
        /// (TurnOn, SetBrightness, etc.). Mirrors the directive's endpoint and correlation
        /// token, and includes a <c>context</c> block with the current state.
        /// </summary>
        public Response Success(string @namespace, string name, object payload, ContextProperty[]? properties)
            => new()
            {
                Event = request.Event(@namespace, name, payload),
                Context = properties?.Length > 0 ? new() { Properties = [.. properties] } : default
            };

        /// <summary>
        /// Build an <c>Alexa.ErrorResponse</c> for a directive the bridge couldn't satisfy.
        /// Interface-specific errors that carry extra payload fields (e.g.
        /// <see cref="ValueOutOfRangeErrorPayload"/>) should construct the
        /// <see cref="ErrorPayload"/> directly and pass it through the event builder
        /// rather than going through this overload.
        /// </summary>
        public Response Error(ErrorType type, string message)
            => new()
            {
                Event = request.Event(Namespaces.Alexa, EventNames.ErrorResponse, new ErrorPayload(type, message)),
                Context = null
            };

        private Event Event(string @namespace, string name, object payload)
            => Core.Event.Create(
                @namespace, name, payload,
                request?.Directive.Endpoint is not null
                    ? new()
                    {
                        EndpointId = request.Directive.Endpoint.EndpointId,
                        Scope = request.Directive.Endpoint.Scope
                    }
                    : null,
                request?.Directive.Header.CorrelationToken.Value,
                request?.Directive.Header.Instance
            );
    }
}
