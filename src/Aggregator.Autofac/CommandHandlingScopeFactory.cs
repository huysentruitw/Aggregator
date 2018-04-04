using System;
using Aggregator.Command;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac implementation for <see cref="ICommandHandlingScopeFactory"/>.
    /// </summary>
    public class CommandHandlingScopeFactory : ICommandHandlingScopeFactory
    {
        private readonly ICommandHandlerTypeLocator _commandHandlerTypeLocator;
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Construct a new <see cref="ICommandHandlingScopeFactory"/> instance.
        /// </summary>
        /// <param name="commandHandlerTypeLocator">The command handler type locator.</param>
        /// <param name="lifetimeScope">The parent Autofac lifetime scope.</param>
        public CommandHandlingScopeFactory(ICommandHandlerTypeLocator commandHandlerTypeLocator, ILifetimeScope lifetimeScope)
        {
            _commandHandlerTypeLocator = commandHandlerTypeLocator ?? throw new ArgumentNullException(nameof(commandHandlerTypeLocator));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
        }

        /// <summary>
        /// Begin a scope for resolving command handlers that handle the specified command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <param name="context">The command handling context.</param>
        /// <returns>The command handling scope.</returns>
        public ICommandHandlingScope<TCommand> BeginScopeFor<TCommand>(CommandHandlingContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var handlerTypes = _commandHandlerTypeLocator.For<TCommand>() ?? Array.Empty<Type>();
            var innerScope = _lifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(context);
                Array.ForEach(handlerTypes, handlerType => builder.RegisterType(handlerType));
            });

            return new CommandHandlingScope<TCommand>(innerScope, handlerTypes);
        }
    }
}
