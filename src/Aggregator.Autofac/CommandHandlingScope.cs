using System;
using System.Linq;
using Aggregator.Command;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac implementation of <see cref="ICommandHandlingScope{TCommand}"/>.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public class CommandHandlingScope<TCommand> : ICommandHandlingScope<TCommand>
    {
        private readonly ILifetimeScope _ownedLifetimeScope;
        private readonly Type[] _handlerTypes;

        internal CommandHandlingScope(ILifetimeScope ownedLifetimeScope, Type[] handlerTypes)
        {
            _ownedLifetimeScope = ownedLifetimeScope ?? throw new ArgumentNullException(nameof(ownedLifetimeScope));
            _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
        }

        /// <summary>
        /// Disposes this command handler scope.
        /// </summary>
        public void Dispose()
        {
            _ownedLifetimeScope.Dispose();
        }

        /// <summary>
        /// Gets all known handlers for the given command type.
        /// </summary>
        /// <returns>All known handlers for the given command type.</returns>
        public ICommandHandler<TCommand>[] ResolveHandlers()
            => _handlerTypes
                .Select(type => (ICommandHandler<TCommand>)_ownedLifetimeScope.Resolve(type))
                .ToArray();
    }
}
