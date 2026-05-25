using System;
using System.Net.Http;
using Lando;
using Lando.Alexa.Security.HMAC;

namespace Microsoft.Extensions.DependencyInjection;

using static Lando.Alexa.Constants.HttpClients;

/// <summary>
/// DI registration helpers for the Alexa core layer (HMAC validation,
/// named HTTP clients, and the keyed request-handler / request-validator pair
/// consumed by <c>FunctionBase&lt;TRequest,TResponse&gt;</c>).
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a <typeparamref name="THandler"/> as the
        /// <see cref="IRequestHandler{TRequest,TResponse}"/> for the keyed
        /// service <paramref name="name"/>, paired with an
        /// <see cref="IRequestValidator"/> backed by
        /// <see cref="HmacSignatureVerifier"/> for the same key.
        /// </summary>
        /// <remarks>
        /// <c>FunctionBase&lt;TRequest,TResponse&gt;</c> resolves both services
        /// by the same key, so registering them as a pair here is what guarantees
        /// every handler entry point is HMAC-gated. The key should be a stable
        /// constant (e.g. <c>Lando.Alexa.SmartHome.Constants.Function.Handler</c>)
        /// — never <c>typeof(THandler).Name</c>, which silently breaks on rename.
        /// </remarks>
        /// <typeparam name="THandler">The concrete handler type to register.</typeparam>
        /// <typeparam name="TRequest">Deserialised request type.</typeparam>
        /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
        /// <param name="name">The DI key shared by the handler and its validator.</param>
        public IServiceCollection AddRequestHandler<THandler, TRequest, TResponse>(string name)
            where THandler : class, IRequestHandler<TRequest, TResponse>
            where TRequest : class
        {
            services.AddKeyedScoped<IRequestHandler<TRequest, TResponse>, THandler>(name);
            services.AddKeyedSingleton<IRequestValidator, HmacSignatureVerifier>(name);
            return services;
        }

        /// <summary>
        /// Registers the cross-cutting Alexa infrastructure: named HTTP clients
        /// for the Amazon and Alexa APIs, plus inbound HMAC verification.
        /// Per-handler registrations should call <see cref="AddRequestHandler"/>
        /// after this.
        /// </summary>
        public IServiceCollection AddAlexa()
        {
            services.AddHttpClients();
            services.AddSecurity();
            return services;
        }

        /// <summary>
        /// Registers the HMAC verification stack: the singleton verifier and the
        /// options binding with startup validation.
        /// </summary>
        private IServiceCollection AddSecurity()
        {
            // HMAC verification for inbound requests from the AWS Lambda proxy.
            // SharedSecret is injected as Hmac__SharedSecret (Key Vault reference) at runtime.
            services.AddOptions<HmacOptions>()
                .BindConfiguration(HmacOptions.SectionName)
                .Validate(o => !string.IsNullOrWhiteSpace(o.SharedSecret), "Hmac:SharedSecret is required")
                .Validate(o => o.MaxClockSkewSeconds is > 0 and <= 900, "Hmac:MaxClockSkewSeconds must be in (0, 900]")
                .ValidateOnStart();
            return services;
        }

        /// <summary>
        /// Registers the named <see cref="HttpClient"/> instances
        /// used to talk to the Amazon and Alexa APIs. Both have a hard 10-second
        /// timeout — these endpoints either respond quickly or won't respond at
        /// all, and we don't want a stuck call to wedge a function invocation.
        /// </summary>
        private IServiceCollection AddHttpClients()
        {
            services.AddHttpClient(AlexaApi.Name, client =>
            {
                client.BaseAddress = new Uri(AlexaApi.Url);
                client.Timeout = TimeSpan.FromSeconds(AlexaApi.TimeoutSeconds);
            });
            services.AddHttpClient(AmazonApi.Name, client =>
            {
                client.BaseAddress = new Uri(AmazonApi.Url);
                client.Timeout = TimeSpan.FromSeconds(AmazonApi.TimeoutSeconds);
            });
            return services;
        }
    }
}
