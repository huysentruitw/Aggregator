using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aggregator.Command
{
    /// <summary>
    /// Reflection based implementation of <see cref="ICommandHandlerTypeLocator"/>.
    /// </summary>
    public class ReflectionCommandHandlerTypeLocator : ICommandHandlerTypeLocator
    {
        private static readonly Type GenericInterfaceType = typeof(ICommandHandler<>);
        private static readonly Lazy<ILookup<Type, Type>> Cache = new Lazy<ILookup<Type, Type>>(BuildCache);

        /// <summary>
        /// Get types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <returns>An array of types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.</returns>
        public Type[] For<TCommand>()
            => Cache.Value[typeof(TCommand)]?.ToArray() ?? Array.Empty<Type>();

        private static ILookup<Type, Type> BuildCache()
        {
            var commandHandlers = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(FindCommandHandlersInAssembly)
                .ToArray();

            return commandHandlers
                .SelectMany(x => x.Commands, (x, command) => (Handler: x.Handler, Command: command))
                .ToLookup(x => x.Command, x => x.Handler);
        }

        private static IEnumerable<(Type Handler, Type[] Commands)> FindCommandHandlersInAssembly(Assembly assembly)
            => assembly.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract)
                .Select(x => (Handler: x, Commands: FindHandledCommandTypes(x)))
                .Where(x => x.Commands.Any());

        private static Type[] FindHandledCommandTypes(Type type)
            => type.GetInterfaces()
                .Where(x => x.IsGenericType && GenericInterfaceType.Equals(x.GetGenericTypeDefinition()))
                .Select(x => x.GenericTypeArguments[0])
                .ToArray();
    }
}
