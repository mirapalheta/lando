using System.Diagnostics.CodeAnalysis;
using Lando.Alexa.CustomSkill.Handlers;
using Lando.Alexa.CustomSkill.Models;
using Lando.Alexa.CustomSkill.Services;

namespace Microsoft.Extensions.DependencyInjection;

using static Lando.Alexa.CustomSkill.Constants;

/// <summary>
/// DI registration for the Alexa Custom Skill (intent) path. Mirrors
/// <c>AddAlexaSmartHome</c>: registers the intent handler under the function's
/// DI key (which also wires the shared HMAC validator via
/// <c>AddRequestHandler</c>), plus the script resolver and its cache.
/// </summary>
/// <remarks>Composition-root wiring only — excluded from coverage.</remarks>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Alexa Custom Skill intent handler, the intent→script
        /// resolver, and the memory cache it uses, onto the service collection.
        /// </summary>
        /// <returns>The same service collection for fluent chaining.</returns>
        public IServiceCollection AddAlexaCustomSkill()
        {
            services.AddMemoryCache();
            services.AddScoped<IIntentScriptResolver, IntentScriptResolver>();
            services.AddRequestHandler<IntentSkillHandler, IntentRequest, IntentResponse>(Function.Handler);
            return services;
        }
    }
}
