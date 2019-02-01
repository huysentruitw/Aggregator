using System;
using System.Diagnostics.CodeAnalysis;
using AggregatR.Command;
using AggregatR.DI;
using AggregatR.Event;
using AggregatR.Persistence;
using Autofac;

namespace AggregatR.Autofac
{
    /// <summary>
    /// Autofac module for the AggregatR library where aggregate root identifiers are of type <see cref="string"/> and the command/event base type is <see cref="object"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AggregatRModule : AggregatRModule<string, object, object>
    {
        /// <summary>
        /// Adds AggregatR related registrations to the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Registrate the non-generic overrides on top of the generic base stuff
            builder.RegisterType<CommandProcessor>().As<ICommandProcessor>().SingleInstance();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
        }
    }

    /// <summary>
    /// Autofac module for the AggregatR library.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    [ExcludeFromCodeCoverage]
    public class AggregatRModule<TIdentifier, TCommandBase, TEventBase> : Module
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Adds AggregatR related registrations to the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommandHandlingContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<CommandProcessor<TIdentifier, TCommandBase, TEventBase>>().As<ICommandProcessor<TCommandBase>>().SingleInstance();
            builder.RegisterGeneric(typeof(Repository<,,>)).As(typeof(IRepository<,,>)).InstancePerLifetimeScope();
            builder.RegisterType<ServiceScopeFactory>().As<IServiceScopeFactory>().SingleInstance();
            builder.RegisterType<EventDispatcher<TEventBase>>().As<IEventDispatcher<TEventBase>>().SingleInstance();
        }
    }
}
