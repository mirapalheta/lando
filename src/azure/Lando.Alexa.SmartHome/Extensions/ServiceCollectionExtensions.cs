using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Lando;
using Lando.Alexa.Security.LWA;
using Lando.Alexa.SmartHome;
using Lando.Alexa.SmartHome.Configuration;
using Lando.Alexa.SmartHome.Handlers;
using Lando.Alexa.SmartHome.Handlers.Directives;
using Lando.Alexa.SmartHome.Models.Authorization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.Alexa.SmartHome.Services;
using Lando.Alexa.SmartHome.Transformers.Entity;
using Lando.Alexa.SmartHome.Transformers.Payload;
using Lando.Alexa.SmartHome.Validators;
using Lando.Alexa.SmartHome.Validators.Payload;
using Lando.Security;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

using static Lando.Alexa.Constants.HttpClients;
using static Lando.Alexa.SmartHome.Constants;
using static Lando.HomeAssistant.Constants;

/// <summary>
/// DI registration helpers for the Alexa Smart Home layer: the per-directive
/// handlers, the per-payload FluentValidation validators, the per-domain
/// entity transformers (discovery + state), and the Login-with-Amazon
/// + Event Gateway plumbing.
/// </summary>
/// <remarks>
/// Composition-root wiring only — no branching logic worth line-covering.
/// The registrations themselves are exercised indirectly by every handler /
/// validator / transformer test; excluded here so DI plumbing doesn't dilute
/// the coverage metric for the actual business logic.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Alexa skill seam — handlers, validators, and per-domain
        /// transformers — onto the application service collection.
        /// </summary>
        /// <remarks>
        /// Idempotent within a single <see cref="IServiceCollection"/> instance: each
        /// concrete service is registered as <c>Transient</c> so callers may resolve
        /// them per request without sharing state.
        /// </remarks>
        /// <returns>The same service collection for fluent chaining.</returns>
        public IServiceCollection AddAlexaSmartHome()
        {
            services.AddOptions<SmartHomeOptions>()
                .BindConfiguration(SmartHomeOptions.SectionName);

            services.AddAlexa();
            services.AddMemoryCache();
            services.AddEventGateway();
            services.AddHandlers();
            services.AddValidators();
            services.AddTransformers();

            // Proactive ChangeReport publisher for state changes.
            services.AddHostedService<ChangeReportService>();
            return services;
        }

        /// <summary>
        /// Registers the Login-with-Amazon HTTP clients used by the AcceptGrant flow
        /// and the Event Gateway publisher. The matching <c>ITokenStore</c>
        /// implementation is supplied by the host project (see
        /// <c>Lando.FunctionApp.Authorization</c>) because the storage layer is
        /// platform-specific (Azure Key Vault).
        /// </summary>
        private IServiceCollection AddEventGateway()
        {
            services.AddKeyedSingleton<ITokenClient>(TokenStore, (provider, _) =>
            {
                var client = provider.GetHttpClient(AmazonApi.Name);
                var credentials = provider.GetRequiredService<IOptions<SmartHomeOptions>>().Value.Event;
                return ActivatorUtilities.CreateInstance<LwaTokenClient>(provider, client, credentials);
            });
            services.AddTokenStore<ITokenClient>(TokenStore);
            services.AddSingleton<IEventGatewayClient>(provider =>
            {
                var client = provider.GetHttpClient(AlexaApi.Name);
                var store = provider.GetRequiredKeyedService<ITokenStore>(TokenStore);
                return ActivatorUtilities.CreateInstance<EventGatewayClient>(provider, client, store);
            });
            return services;
        }

        /// <summary>
        /// Registers the per-domain transformers (both <c>DiscoveryEndpoint</c>
        /// and <c>IReadOnlyList&lt;ContextProperty&gt;</c> projections) plus the
        /// composite <see cref="EntityTransform"/> dispatcher that fans entities
        /// out to them by domain.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All transformers are stateless pure functions, so singleton
        /// registrations are correct and reduce per-directive allocation. The
        /// dispatcher is registered once as the concrete type and surfaced
        /// through both closed-generic interfaces via factory delegates so
        /// consumers get a single shared instance.
        /// </para>
        /// <para>
        /// Adding a new domain is two keyed registrations (one for each output
        /// shape) plus the two transformer classes — no new interface, no new
        /// dispatcher.
        /// </para>
        /// </remarks>
        /// <returns>The same service collection for fluent chaining.</returns>
        private IServiceCollection AddTransformers()
        {
            services.AddSingleton<EntityTransform>();
            services.AddSingleton<IEntityTransform<DiscoveryEndpoint>>(
                p => p.GetRequiredService<EntityTransform>());
            services.AddSingleton<IEntityTransform<ContextProperty[]>>(
                p => p.GetRequiredService<EntityTransform>());

            services.AddEntityTransform<ClimateDiscoveryTransformer, ClimateStateTransformer>(Domains.Climate);
            services.AddEntityTransform<CoverDiscoveryTransformer, CoverStateTransformer>(Domains.Cover);
            services.AddEntityTransform<FanDiscoveryTransformer, FanStateTransformer>(Domains.Fan);
            services.AddEntityTransform<LightDiscoveryTransformer, LightStateTransformer>(Domains.Light);
            services.AddEntityTransform<LockDiscoveryTransformer, LockStateTransformer>(Domains.Lock);
            services.AddEntityTransform<MediaPlayerDiscoveryTransformer, MediaPlayerStateTransformer>(Domains.MediaPlayer);
            services.AddEntityTransform<SceneDiscoveryTransformer, SceneControllerStateTransformer>(Domains.Scene);
            services.AddEntityTransform<ScriptDiscoveryTransformer, SceneControllerStateTransformer>(Domains.Script);
            services.AddEntityTransform<SensorDiscoveryTransformer, SensorStateTransformer>(Domains.Sensor);
            services.AddEntityTransform<SwitchDiscoveryTransformer, SwitchStateTransformer>(Domains.Switch);
            return services;
        }

        private IServiceCollection AddEntityTransform<TDiscovery, TState>(string domain)
            where TDiscovery : class, IEntityTransform<DiscoveryEndpoint>
            where TState : class, IEntityTransform<ContextProperty[]>
            => services
                .AddKeyedSingleton<IEntityTransform<DiscoveryEndpoint>, TDiscovery>(domain)
                .AddKeyedSingleton<IEntityTransform<ContextProperty[]>, TState>(domain);

        private IServiceCollection AddHandlers()
        {
            services.AddRequestHandler<SmartHomeHandler, Request, Response>(Function.Handler);

            services.AddKeyedTransient<IDirectiveHandler, AcceptGrantDirectiveHandler>(DirectiveNames.AcceptGrant);
            services.AddKeyedTransient<IDirectiveHandler, DiscoverDirectiveHandler>(DirectiveNames.Discover);
            services.AddKeyedTransient<IDirectiveHandler, ReportStateDirectiveHandler>(DirectiveNames.ReportState);
            services.AddKeyedTransient<IDirectiveHandler, ResumeScheduleDirectiveHandler>(DirectiveNames.ResumeSchedule);

            services.AddControlDirectiveHandler<AdjustBrightnessPayload, AdjustBrightnessPayloadTransform>(DirectiveNames.AdjustBrightness);
            services.AddControlDirectiveHandler<AdjustPercentagePayload, AdjustPercentagePayloadTransform>(DirectiveNames.AdjustPercentage);
            services.AddControlDirectiveHandler<AdjustRangeValuePayload, AdjustRangeValuePayloadTransform>(DirectiveNames.AdjustRangeValue);
            services.AddControlDirectiveHandler<AdjustTargetTemperaturePayload, AdjustTargetTemperaturePayloadTransform>(DirectiveNames.AdjustTargetTemperature);
            services.AddControlDirectiveHandler<AdjustVolumePayload, AdjustVolumePayloadTransform>(DirectiveNames.AdjustVolume);
            services.AddControlDirectiveHandler<EmptyPayload, DecreaseColorTemperaturePayloadTransform>(DirectiveNames.DecreaseColorTemperature);
            services.AddControlDirectiveHandler<EmptyPayload, IncreaseColorTemperaturePayloadTransform>(DirectiveNames.IncreaseColorTemperature);
            services.AddControlDirectiveHandler<EmptyPayload, LockPayloadTransform>(DirectiveNames.Lock);
            services.AddControlDirectiveHandler<SetBrightnessPayload, SetBrightnessPayloadTransform>(DirectiveNames.SetBrightness);
            services.AddControlDirectiveHandler<SetColorPayload, SetColorPayloadTransform>(DirectiveNames.SetColor);
            services.AddControlDirectiveHandler<SetColorTemperaturePayload, SetColorTemperaturePayloadTransform>(DirectiveNames.SetColorTemperature);
            services.AddControlDirectiveHandler<SetMutePayload, SetMutePayloadTransform>(DirectiveNames.SetMute);
            services.AddControlDirectiveHandler<SetPercentagePayload, SetPercentagePayloadTransform>(DirectiveNames.SetPercentage);
            services.AddControlDirectiveHandler<SetRangeValuePayload, SetRangeValuePayloadTransform>(DirectiveNames.SetRangeValue);
            services.AddControlDirectiveHandler<SetTargetTemperaturePayload, SetTargetTemperaturePayloadTransform>(DirectiveNames.SetTargetTemperature);
            services.AddControlDirectiveHandler<SetThermostatModePayload, SetThermostatModePayloadTransform>(DirectiveNames.SetThermostatMode);
            services.AddControlDirectiveHandler<SetVolumePayload, SetVolumePayloadTransform>(DirectiveNames.SetVolume);
            services.AddControlDirectiveHandler<EmptyPayload, TurnOffPayloadTransform>(DirectiveNames.TurnOff);
            services.AddControlDirectiveHandler<EmptyPayload, TurnOnPayloadTransform>(DirectiveNames.TurnOn);
            services.AddControlDirectiveHandler<EmptyPayload, UnlockPayloadTransform>(DirectiveNames.Unlock);

            services.AddSceneDirectiveHandler<EmptyPayload, TurnOnPayloadTransform>(DirectiveNames.Activate, EventNames.ActivationStarted);
            services.AddSceneDirectiveHandler<EmptyPayload, TurnOffPayloadTransform>(DirectiveNames.Deactivate, EventNames.DeactivationStarted);
            return services;
        }

        private IServiceCollection AddControlDirectiveHandler<TPayload, TTransform>(string directiveName)
            where TPayload : class
            where TTransform : class, IPayloadTransform<TPayload>
            => services
                .AddKeyedSingleton<IPayloadTransform<TPayload>, TTransform>(directiveName)
                .AddKeyedTransient<IDirectiveHandler, ControlDirectiveHandler<TPayload>>(
                    directiveName,
                    (provider, key) => ActivatorUtilities.CreateInstance<ControlDirectiveHandler<TPayload>>(provider, key!)
                );

        /// <summary>
        /// Registers a <see cref="SceneDirectiveHandler"/> for an
        /// <c>Alexa.SceneController</c> directive: keys the payload transform and the
        /// handler under <paramref name="directiveName"/> (Activate/Deactivate) and
        /// supplies the <paramref name="eventName"/> (ActivationStarted/DeactivationStarted)
        /// the handler echoes back. Reuses the same dispatch plumbing as
        /// <see cref="AddControlDirectiveHandler{TPayload,TTransform}"/>; only the
        /// response namespace/name/payload differ.
        /// </summary>
        private IServiceCollection AddSceneDirectiveHandler<TPayload, TTransform>(string directiveName, string eventName)
            where TPayload : class
            where TTransform : class, IPayloadTransform<TPayload>
            => services
                .AddKeyedSingleton<IPayloadTransform<TPayload>, TTransform>(directiveName)
                .AddKeyedTransient<IDirectiveHandler, SceneDirectiveHandler>(
                    directiveName,
                    (provider, key) => ActivatorUtilities.CreateInstance<SceneDirectiveHandler>(provider, key!, eventName)
                );

        private IServiceCollection AddValidators()
        {
            services.AddSingleton<ITokenValidator, TokenValidator>();
            services.AddTransient<IValidator<Request>, RequestValidator>();
            services.AddTransient<IValidator<EmptyPayload>, EmptyPayloadValidator>();
            services.AddTransient<IValidator<AcceptGrantPayload>, AcceptGrantPayloadValidator>();
            services.AddTransient<IValidator<AdjustBrightnessPayload>, AdjustBrightnessPayloadValidator>();
            services.AddTransient<IValidator<AdjustPercentagePayload>, AdjustPercentagePayloadValidator>();
            services.AddTransient<IValidator<AdjustRangeValuePayload>, AdjustRangeValuePayloadValidator>();
            services.AddTransient<IValidator<AdjustTargetTemperaturePayload>, AdjustTargetTemperaturePayloadValidator>();
            services.AddTransient<IValidator<AdjustVolumePayload>, AdjustVolumePayloadValidator>();
            services.AddTransient<IValidator<DiscoveryDirectivePayload>, DiscoveryDirectivePayloadValidator>();
            services.AddTransient<IValidator<ResumeSchedulePayload>, ResumeSchedulePayloadValidator>();
            services.AddTransient<IValidator<SetBrightnessPayload>, SetBrightnessPayloadValidator>();
            services.AddTransient<IValidator<SetColorPayload>, SetColorPayloadValidator>();
            services.AddTransient<IValidator<SetColorTemperaturePayload>, SetColorTemperaturePayloadValidator>();
            services.AddTransient<IValidator<SetMutePayload>, SetMutePayloadValidator>();
            services.AddTransient<IValidator<SetPercentagePayload>, SetPercentagePayloadValidator>();
            services.AddTransient<IValidator<SetRangeValuePayload>, SetRangeValuePayloadValidator>();
            services.AddTransient<IValidator<SetTargetTemperaturePayload>, SetTargetTemperaturePayloadValidator>();
            services.AddTransient<IValidator<SetThermostatModePayload>, SetThermostatModePayloadValidator>();
            services.AddTransient<IValidator<SetVolumePayload>, SetVolumePayloadValidator>();
            return services;
        }
    }
}
