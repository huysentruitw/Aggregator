using System;
using System.Diagnostics.CodeAnalysis;
using Aggregator.Command;
using Aggregator.Event;
using Aggregator.Persistence;
using Autofac;

namespace Aggregator.Autofac
{
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
            builder.RegisterType<ReflectionCommandHandlerTypeLocator>().As<ICommandHandlerTypeLocator>().SingleInstance();
            builder.RegisterType<CommandHandlingScopeFactory>().As<ICommandHandlingScopeFactory>().SingleInstance();
            builder.RegisterType<CommandProcessor<TIdentifier, TCommandBase, TEventBase>>().AsSelf().SingleInstance();
            builder.RegisterGeneric(typeof(Repository<,,>)).AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<ReflectionEventHandlerTypeLocator>().As<IEventHandlerTypeLocator>().SingleInstance();
            builder.RegisterType<EventHandlingScopeFactory>().As<IEventHandlingScopeFactory>().SingleInstance();
            builder.RegisterType<EventDispatcher<TEventBase>>().As<IEventDispatcher<TEventBase>>().SingleInstance();
        }
    }
}
