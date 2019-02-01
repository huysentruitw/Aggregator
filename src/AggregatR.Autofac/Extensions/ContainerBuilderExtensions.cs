using AggregatR.Command;
using AggregatR.DI;
using AggregatR.Event;

namespace Autofac
{
    /// <summary>
    /// Extension methods for Autofac <see cref="ContainerBuilder"/>.
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Registers all <see cref="ICommandHandler{TCommand}"/> implementations found in the given assembly.
        /// </summary>
        /// <typeparam name="T">The type used to identify the assembly.</typeparam>
        /// <param name="builder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <returns>The <see cref="ContainerBuilder"/> instance.</returns>
        public static ContainerBuilder RegisterCommandHandlersInAssemblyOf<T>(this ContainerBuilder builder)
        {
            foreach (var implementationType in ReflectionTypeLocator.Locate(typeof(ICommandHandler<>), typeof(T).Assembly))
                builder.RegisterType(implementationType).AsImplementedInterfaces().InstancePerLifetimeScope();
            return builder;
        }

        /// <summary>
        /// Registers all <see cref="IEventHandler{TEvent}"/> implementations found in the given assembly.
        /// </summary>
        /// <typeparam name="T">The type used to identify the assembly.</typeparam>
        /// <param name="builder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <returns>The <see cref="ContainerBuilder"/> instance.</returns>
        public static ContainerBuilder RegisterEventHandlersInAssemblyOf<T>(this ContainerBuilder builder)
        {
            foreach (var implementationType in ReflectionTypeLocator.Locate(typeof(IEventHandler<>), typeof(T).Assembly))
                builder.RegisterType(implementationType).AsImplementedInterfaces().InstancePerLifetimeScope();
            return builder;
        }
    }
}
