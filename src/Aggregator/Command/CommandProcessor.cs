using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.DI;
using Aggregator.Event;
using Aggregator.Exceptions;
using Aggregator.Persistence;

namespace Aggregator.Command
{
    /// <summary>
    /// This class is responsible for processing commands where the aggregate root identifier is a <see cref="string"/> and commands/events derive from <see cref="object"/>.
    /// Should be used as a singleton.
    /// </summary>
    public class CommandProcessor : CommandProcessor<string, object, object>, ICommandProcessor
    {
        /// <summary>
        /// Constructs a new <see cref="CommandProcessor"/> instance.
        /// </summary>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers"/> instance.</param>
        public CommandProcessor(
            IServiceScopeFactory serviceScopeFactory,
            IEventStore<string, object> eventStore,
            IEventDispatcher<object> eventDispatcher,
            CommandProcessorNotificationHandlers notificationHandlers = null)
            : base(serviceScopeFactory, eventStore, eventDispatcher, notificationHandlers)
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
        private readonly IEventStore<TIdentifier, TEventBase> _eventStore;
        private readonly IEventDispatcher<TEventBase> _eventDispatcher;
        private readonly CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> _notificationHandlers;

        /// <summary>
        /// Constructs a new <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/> instance.
        /// </summary>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers{TIdentifier, TCommandBase, TEventBase}"/> instance.</param>
        public CommandProcessor(
            IServiceScopeFactory serviceScopeFactory,
            IEventStore<TIdentifier, TEventBase> eventStore,
            IEventDispatcher<TEventBase> eventDispatcher,
            CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> notificationHandlers = null)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _notificationHandlers = notificationHandlers ?? new CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase>();
        }

        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="cancellationToken">A cancellation token that allows cancelling the process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Process(TCommandBase command, CancellationToken cancellationToken = default)
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
                        ?.MakeGenericMethod(type))
                    ?? throw new InvalidOperationException($"Couldn't make generic {nameof(Execute)} method");

                await ((Task)executeMethod.Invoke(this, new object[] { command, serviceScope, cancellationToken })).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (!unitOfWork.HasChanges)
                {
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
                            await transaction.StoreEvents(aggregateRootEntity.Identifier, aggregateRootEntity.ExpectedVersion, events, cancellationToken).ConfigureAwait(false);
                            storedEvents.AddRange(events);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        await _eventDispatcher.Dispatch(storedEvents.ToArray(), cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

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

        private async Task Execute<TCommand>(TCommand command, IServiceScope serviceScope, CancellationToken cancellationToken)
            where TCommand : TCommandBase
        {
            var handlers = serviceScope.GetServices<ICommandHandler<TCommand>>()?.ToArray();

            if (handlers == null || !handlers.Any())
                throw new UnhandledCommandException(command);

            foreach (var handler in handlers)
                await handler.Handle(command, cancellationToken).ConfigureAwait(false);
        }
    }
}
