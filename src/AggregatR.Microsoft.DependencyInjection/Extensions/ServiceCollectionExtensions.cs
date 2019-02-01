using System;
using System.Diagnostics.CodeAnalysis;
using AggregatR.Command;
using AggregatR.Event;
using AggregatR.Microsoft.DependencyInjection;
using AggregatR.Persistence;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// AggregatR related extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method that registers dependencies for the AggregatR library.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddAggregatR(this IServiceCollection services)
        {
            // Registrate the non-generic overrides on top of the generic base stuff
            services.AddSingleton<ICommandProcessor, CommandProcessor>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            return services.AddAggregatR<string, object, object>();
        }

        /// <summary>
        /// Extension method that registers dependencies for the AggregatR library.
        /// </summary>
        /// <typeparam name="TIdentifier">The identifier type.</typeparam>
        /// <typeparam name="TCommandBase">The command base type.</typeparam>
        /// <typeparam name="TEventBase">The event base type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddAggregatR<TIdentifier, TCommandBase, TEventBase>(this IServiceCollection services)
            where TIdentifier : IEquatable<TIdentifier>
        {
            services.AddScoped<CommandHandlingContext>();
            services.AddSingleton<ICommandProcessor<TCommandBase>, CommandProcessor<TIdentifier, TCommandBase, TEventBase>>();
            services.AddScoped(typeof(IRepository<,,>), typeof(Repository<,,>));
            services.AddSingleton<AggregatR.DI.IServiceScopeFactory, ServiceScopeFactory>();
            services.AddSingleton<IEventDispatcher<TEventBase>, EventDispatcher<TEventBase>>();
            return services;
        }
    }
}
