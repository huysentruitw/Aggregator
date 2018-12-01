using Aggregator.Command;
using Aggregator.DI;
using Aggregator.Event;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterCommandHandlersInAssemblyOf<T>(this ContainerBuilder builder)
        {
            foreach (var implementationType in ReflectionTypeLocator.Locate(typeof(ICommandHandler<>), typeof(T).Assembly))
                builder.RegisterType(implementationType).AsImplementedInterfaces();
            return builder;
        }

        public static ContainerBuilder RegisterEventHandlersInAssemblyOf<T>(this ContainerBuilder builder)
        {
            foreach (var implementationType in ReflectionTypeLocator.Locate(typeof(IEventHandler<>), typeof(T).Assembly))
                builder.RegisterType(implementationType).AsImplementedInterfaces();
            return builder;
        }
    }
}
