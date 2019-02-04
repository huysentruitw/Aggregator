using System;
using System.Diagnostics.CodeAnalysis;
using Aggregator.Command;
using Aggregator.DI;
using Aggregator.Event;
using Aggregator.Persistence;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac module for the Aggregator library where aggregate root identifiers are of type <see cref="string"/> and commands/events derive from <see cref="object"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AggregatorModule : AggregatorModule<string, object, object>
    {
        /// <summary>
        /// Adds Aggregator related registrations to the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Register the non-generic overrides on top of the generic base stuff
            builder.RegisterType<CommandProcessor>().As<ICommandProcessor>().SingleInstance();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
        }
    }

    /// <summary>
    /// Autofac module for the Aggregator library.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    [ExcludeFromCodeCoverage]
    public class AggregatorModule<TIdentifier, TCommandBase, TEventBase> : Module
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Adds Aggregator related registrations to the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommandHandlingContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<CommandProcessor<TIdentifier, TCommandBase, TEventBase>>()
                .As<ICommandProcessor<TCommandBase>>().SingleInstance();
            builder.RegisterType<EventDispatcher<TEventBase>>()
                .As<IEventDispatcher<TEventBase>>().SingleInstance();
            builder.RegisterGeneric(typeof(Repository<,,>)).As(typeof(IRepository<,,>)).InstancePerLifetimeScope();
            builder.RegisterType<ServiceScopeFactory>().As<IServiceScopeFactory>().SingleInstance();
        }
    }
}
