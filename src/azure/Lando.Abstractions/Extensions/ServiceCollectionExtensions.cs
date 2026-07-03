using System;
using System.Diagnostics.CodeAnalysis;
using Lando.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI helpers for the <c>Lando.Abstractions</c> token-store family. Concrete
/// stores live in the FunctionApp project (where the Azure-aware persistence
/// implementation sits); this layer just owns the registration shape.
/// </summary>
/// <remarks>Composition-root wiring only — excluded from coverage.</remarks>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a keyed <see cref="ITokenStore"/> under <paramref name="name"/>,
        /// resolving its <see cref="ITokenClient"/> dependency from the container
        /// (the concrete <typeparamref name="T"/> must itself be registered).
        /// </summary>
        /// <typeparam name="T">
        /// The concrete <see cref="ITokenClient"/> implementation backing the store.
        /// </typeparam>
        /// <param name="name">
        /// The DI key under which the store is registered. Used by callers to
        /// disambiguate multiple token populations (e.g. one per skill).
        /// </param>
        public IServiceCollection AddTokenStore<T>(string name)
            where T : class, ITokenClient
            => services.AddTokenStore(name, provider => provider.GetRequiredKeyedService<T>(name));

        /// <summary>
        /// Registers a keyed <see cref="ITokenStore"/> under <paramref name="name"/>,
        /// resolving its <see cref="ITokenClient"/> using the same key.
        /// This is a convenience overload for the common case where the store's
        /// <see cref="ITokenClient"/> is registered under the same key.
        /// </summary>
        /// <param name="name">The DI key under which the store is registered.</param>
        /// <returns>The same service collection for fluent chaining.</returns>
        public IServiceCollection AddTokenStore(string name)
            => services.AddTokenStore(name, provider => provider.GetRequiredKeyedService<ITokenClient>(name));

        /// <summary>
        /// Registers a keyed <see cref="ITokenStore"/> under <paramref name="name"/>,
        /// using <paramref name="clientFactory"/> to build the
        /// <see cref="ITokenClient"/> that backs token refreshes. The store itself
        /// is constructed lazily via the registered
        /// <see cref="ITokenStoreFactory"/>.
        /// </summary>
        /// <param name="name">DI key under which the store is registered.</param>
        /// <param name="clientFactory">
        /// Factory invoked once per resolution to produce the
        /// <see cref="ITokenClient"/> the store delegates to.
        /// </param>
        public IServiceCollection AddTokenStore(string name, Func<IServiceProvider, ITokenClient> clientFactory)
            => services.AddKeyedSingleton(name, (provider, key) =>
            {
                var factory = provider.GetRequiredService<ITokenStoreFactory>();
                return factory.Create(name, clientFactory(provider));
            });
    }
}
