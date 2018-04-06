using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aggregator.Event;
using Aggregator.Exceptions;
using Aggregator.Internal;
using Aggregator.Persistence;

namespace Aggregator.Command
{
    /// <summary>
    /// This class is responsibly for processing commands. Should be used as a singleton.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public class CommandProcessor<TIdentifier, TCommandBase, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly ConcurrentDictionary<Type, MethodInfo> _executeMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ICommandHandlingScopeFactory _commandHandlingScopeFactory;
        private readonly IEventDispatcher<TEventBase> _eventDispatcher;
        private readonly IEventStore<TIdentifier, TEventBase> _eventStore;

        /// <summary>
        /// Constructs a new <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/> instance.
        /// </summary>
        /// <param name="commandHandlingScopeFactory">The command handling scope factory.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        /// <param name="eventStore">The event store.</param>
        public CommandProcessor(
            ICommandHandlingScopeFactory commandHandlingScopeFactory,
            IEventDispatcher<TEventBase> eventDispatcher,
            IEventStore<TIdentifier, TEventBase> eventStore)
        {
            _commandHandlingScopeFactory = commandHandlingScopeFactory ?? throw new ArgumentNullException(nameof(commandHandlingScopeFactory));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Process(TCommandBase command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var context = new CommandHandlingContext();

            var unitOfWork = new UnitOfWork<TIdentifier, TEventBase>();
            context.SetUnitOfWork(unitOfWork);

            var executeMethod = _executeMethodCache.GetOrAdd(command.GetType(), type =>
                typeof(CommandProcessor<TIdentifier, TCommandBase, TEventBase>)
                    .GetMethod(nameof(Execute), BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(type));

            await ((Task)executeMethod.Invoke(this, new object[] { command, context })).ConfigureAwait(false);

            if (!unitOfWork.HasChanges)
            {
                await _eventDispatcher.Dispatch(Array.Empty<TEventBase>()).ConfigureAwait(false);
                return;
            }

            using (var transaction = _eventStore.BeginTransaction(context))
            {
                var storedEvents = new List<TEventBase>();

                try
                {
                    foreach (var aggregateRoot in unitOfWork.GetChanges())
                    {
                        var events = aggregateRoot.GetChanges();
                        await transaction.StoreEvents(aggregateRoot.Identifier, aggregateRoot.ExpectedVersion, events).ConfigureAwait(false);
                        storedEvents.AddRange(events);
                    }

                    await transaction.Commit().ConfigureAwait(false);
                }
                catch
                {
                    await transaction.Rollback().ConfigureAwait(false);
                    throw;
                }

                await _eventDispatcher.Dispatch(storedEvents.ToArray()).ConfigureAwait(false);
            }
        }

        private async Task Execute<TCommand>(TCommand command, CommandHandlingContext context)
            where TCommand : TCommandBase
        {
            using (var commandHandlingScope = _commandHandlingScopeFactory.BeginScopeFor<TCommand>(context))
            {
                var handlers = commandHandlingScope.ResolveHandlers();

                if (handlers == null || !handlers.Any())
                    throw new UnhandledCommandException(command);

                foreach (var handler in handlers)
                    await handler.Handle(command).ConfigureAwait(false);
            }
        }
    }
}
