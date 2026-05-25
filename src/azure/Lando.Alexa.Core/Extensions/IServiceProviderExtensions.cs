using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceProvider"/> conveniences used throughout the bridge.
/// </summary>
public static class IServiceProviderExtensions
{
    extension(IServiceProvider provider)
    {
        /// <summary>
        /// Resolves a named <see cref="HttpClient"/> via <see cref="IHttpClientFactory"/>.
        /// Equivalent to <c>provider.GetRequiredService&lt;IHttpClientFactory&gt;().CreateClient(name)</c>,
        /// but reads more naturally at the call site.
        /// </summary>
        /// <param name="name">
        /// The named client registered via <c>services.AddHttpClient(name, …)</c>.
        /// See <c>Lando.Alexa.Constants.HttpClients</c> for the canonical names.
        /// </param>
        public HttpClient GetHttpClient(string name)
            => provider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
    }
}
