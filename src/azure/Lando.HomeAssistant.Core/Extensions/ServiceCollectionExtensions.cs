using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Configuration;
using Lando.HomeAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// DI registration helpers for the Home Assistant integration: configuration
/// binding, the keyed <see cref="HttpClient"/> and
/// <see cref="SocketsHttpHandler"/> stack (proxy + custom CA
/// + bearer auth), and the discovery / service-caller / WebSocket services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the full Home Assistant integration: configuration binding,
        /// keyed HTTP clients with proxy + custom-CA support, the REST-based
        /// discovery and service-caller services, the composite
        /// <see cref="IHomeAssistantClient"/>, and the WebSocket state-change
        /// subscriber.
        /// </summary>
        public IServiceCollection AddHomeAssistant()
        {
            services.AddHttpClient();

            services.AddOptions<HomeAssistantConfiguration>()
                .BindConfiguration(HomeAssistantConfiguration.SectionName);

            services.AddSingleton(provider => provider.GetRequiredService<IOptions<HomeAssistantConfiguration>>().Value.ClientOptions);

            services.AddClients();
            services.AddScoped<IDeviceDiscovery, DeviceDiscoveryService>();
            services.AddScoped<IServiceCaller, ServiceCallerService>();
            services.AddScoped<IHomeAssistantClient, HomeAssistantClient>();
            services.AddSingleton<IHomeAssistantWebSocketClient, HomeAssistantWebSocketClient>();

            return services;
        }

        private IServiceCollection AddClients()
        {
            services.AddKeyedSingleton<IWebProxy>(HomeAssistant, (provider, _) =>
            {
                var options = provider.GetRequiredService<HomeAssistantClientOptions>();
                return string.IsNullOrWhiteSpace(options.ProxyAddress)
                    ? default!
                    : new WebProxy(options.ProxyAddress);
            });
            services.AddKeyedSingleton(HomeAssistant, (provider, _) =>
            {
                var options = provider.GetRequiredService<HomeAssistantClientOptions>();
                return string.IsNullOrWhiteSpace(options.Certificate)
                    ? default!
                    : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(options.Certificate));
            });
            services.AddKeyedSingleton(HomeAssistant, (provider, HomeAssistant) =>
            {
                var proxy = provider.GetKeyedService<IWebProxy>(HomeAssistant);
                var certificate = provider.GetKeyedService<X509Certificate2>(HomeAssistant);

                var handler = new SocketsHttpHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                if (certificate is not null)
                {
                    var logger = provider.GetRequiredService<ILogger<HomeAssistantClient>>();
                    handler.SslOptions = new()
                    {
                        RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                            cert.IsValid(certificate, logger)
                    };
                }
                return handler;
            });
            services.AddKeyedSingleton(HomeAssistant, (provider, _) =>
            {
                var options = provider.GetRequiredService<HomeAssistantClientOptions>();
                var proxy = provider.GetKeyedService<IWebProxy>(HomeAssistant);
                var certificate = provider.GetKeyedService<X509Certificate2>(HomeAssistant);

                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                if (certificate is not null)
                {
                    var logger = provider.GetRequiredService<ILogger<HomeAssistantClient>>();
                    handler.ServerCertificateCustomValidationCallback = (message, cert, _, errors) =>
                        cert.IsValid(message, certificate, logger);
                }
                var client = new HttpClient(handler)
                {
                    BaseAddress = options.ApiUri(),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Lando-HomeAssistantClient/1.0");
                client.DefaultRequestHeaders.Authorization = new("Bearer", options.Token);
                return client;
            });
            return services;
        }
    }
}
