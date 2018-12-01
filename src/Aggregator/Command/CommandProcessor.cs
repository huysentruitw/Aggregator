using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aggregator.DI;
using Aggregator.Event;
using Aggregator.Exceptions;
using Aggregator.Persistence;

namespace Aggregator.Command
{
    /// <summary>
    /// This class is responsible for processing commands where the aggregate root identifier is a <see cref="string"/> and the command/event base type is <see cref="object"/>.
    /// Should be used as a singleton.
    /// </summary>
    public class CommandProcessor : CommandProcessor<string, object, object>, ICommandProcessor
    {
        /// <summary>
        /// Constructs a new <see cref="CommandProcessor"/> instance.
        /// </summary>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers"/> instance.</param>
        public CommandProcessor(
            IServiceScopeFactory serviceScopeFactory,
            IEventDispatcher<object> eventDispatcher,
            IEventStore<string, object> eventStore,
            CommandProcessorNotificationHandlers notificationHandlers = null)
            : base(serviceScopeFactory, eventDispatcher, eventStore, notificationHandlers)
        {
        }
    }

    /// <summary>
    /// This class is responsibly for processing commands.
    /// Should be used as a singleton.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public class CommandProcessor<TIdentifier, TCommandBase, TEventBase> : ICommandProcessor<TCommandBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly ConcurrentDictionary<Type, MethodInfo> _executeMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IEventDispatcher<TEventBase> _eventDispatcher;
        private readonly IEventStore<TIdentifier, TEventBase> _eventStore;
        private readonly CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> _notificationHandlers;

        /// <summary>
        /// Constructs a new <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/> instance.
        /// </summary>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers{TIdentifier, TCommandBase, TEventBase}"/> instance.</param>
        public CommandProcessor(
            IServiceScopeFactory serviceScopeFactory,
            IEventDispatcher<TEventBase> eventDispatcher,
            IEventStore<TIdentifier, TEventBase> eventStore,
            CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> notificationHandlers = null)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _notificationHandlers = notificationHandlers ?? new CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase>();
        }

        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Process(TCommandBase command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var serviceScope = _serviceScopeFactory.CreateScope())
            {
                var context = serviceScope.GetService<CommandHandlingContext>();
                _notificationHandlers.OnPrepareContext(command, context);

                var unitOfWork = context.CreateUnitOfWork<TIdentifier, TEventBase>();

                var executeMethod = _executeMethodCache.GetOrAdd(command.GetType(), type =>
                    typeof(CommandProcessor<TIdentifier, TCommandBase, TEventBase>)
                        .GetMethod(nameof(Execute), BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(type));

                await ((Task)executeMethod.Invoke(this, new object[] { command, serviceScope })).ConfigureAwait(false);

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
                        foreach (var aggregateRootEntity in unitOfWork.GetChanges())
                        {
                            var events = aggregateRootEntity.GetChanges();
                            events = events.Select(x => _notificationHandlers.OnEnrichEvent(x, command, context)).ToArray();
                            await transaction.StoreEvents(aggregateRootEntity.Identifier, aggregateRootEntity.ExpectedVersion, events).ConfigureAwait(false);
                            storedEvents.AddRange(events);
                        }

                        await _eventDispatcher.Dispatch(storedEvents.ToArray()).ConfigureAwait(false);

                        await transaction.Commit().ConfigureAwait(false);
                    }
                    catch
                    {
                        await transaction.Rollback().ConfigureAwait(false);
                        throw;
                    }
                }
            }
        }

        private async Task Execute<TCommand>(TCommand command, IServiceScope serviceScope)
            where TCommand : TCommandBase
        {
            var handlers = serviceScope.GetServices<ICommandHandler<TCommand>>();

            if (handlers == null || !handlers.Any())
                throw new UnhandledCommandException(command);

            foreach (var handler in handlers)
                await handler.Handle(command).ConfigureAwait(false);
        }
    }
}
