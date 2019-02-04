using System;
using System.Diagnostics.CodeAnalysis;
using Aggregator.Command;
using Aggregator.Event;
using Aggregator.Microsoft.DependencyInjection;
using Aggregator.Persistence;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Aggregator related extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method that registers dependencies for the Aggregator library.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddAggregator(this IServiceCollection services)
        {
            // Register the non-generic overrides on top of the generic base stuff
            services.AddSingleton<ICommandProcessor, CommandProcessor>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            return services.AddAggregator<string, object, object>();
        }

        /// <summary>
        /// Extension method that registers dependencies for the Aggregator library.
        /// </summary>
        /// <typeparam name="TIdentifier">The identifier type.</typeparam>
        /// <typeparam name="TCommandBase">The command base type.</typeparam>
        /// <typeparam name="TEventBase">The event base type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddAggregator<TIdentifier, TCommandBase, TEventBase>(this IServiceCollection services)
            where TIdentifier : IEquatable<TIdentifier>
        {
            services.AddScoped<CommandHandlingContext>();
            services.AddSingleton<ICommandProcessor<TCommandBase>, CommandProcessor<TIdentifier, TCommandBase, TEventBase>>();
            services.AddSingleton<IEventDispatcher<TEventBase>, EventDispatcher<TEventBase>>();
            services.AddScoped(typeof(IRepository<,,>), typeof(Repository<,,>));
            services.AddSingleton<Aggregator.DI.IServiceScopeFactory, ServiceScopeFactory>();
            return services;
        }
    }
}
