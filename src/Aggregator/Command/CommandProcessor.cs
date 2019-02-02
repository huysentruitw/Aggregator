using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.DI;
using Aggregator.Exceptions;
using Aggregator.Persistence;
using MediatR;

namespace Aggregator.Command
{
    /// <summary>
    /// This class is responsible for processing commands where the aggregate root identifier is a <see cref="string"/>, the commands implement <see cref="ICommand"/> and the events implement <see cref="IEvent"/>.
    /// Should be used as a singleton.
    /// </summary>
    public class CommandProcessor : CommandProcessor<string, ICommand, IEvent>, ICommandProcessor
    {
        /// <summary>
        /// Constructs a new <see cref="CommandProcessor"/> instance.
        /// </summary>
        /// <param name="mediator">The mediator instance.</param>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers"/> instance.</param>
        public CommandProcessor(
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            IEventStore<string, IEvent> eventStore,
            CommandProcessorNotificationHandlers notificationHandlers = null)
            : base(mediator, serviceScopeFactory, eventStore, notificationHandlers)
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
        where TCommandBase : ICommand
        where TEventBase : IEvent
    {
        private readonly IMediator _mediator;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IEventStore<TIdentifier, TEventBase> _eventStore;
        private readonly CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> _notificationHandlers;

        /// <summary>
        /// Constructs a new <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/> instance.
        /// </summary>
        /// <param name="mediator">The mediator interface.</param>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        /// <param name="eventStore">The event store.</param>
        /// <param name="notificationHandlers">Optional <see cref="CommandProcessorNotificationHandlers{TIdentifier, TCommandBase, TEventBase}"/> instance.</param>
        public CommandProcessor(
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            IEventStore<TIdentifier, TEventBase> eventStore,
            CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase> notificationHandlers = null)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _notificationHandlers = notificationHandlers ?? new CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase>();
        }

        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="cancellationToken">A cancellation token that allows cancelling the process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Process(TCommandBase command, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var serviceScope = _serviceScopeFactory.CreateScope())
            {
                var context = serviceScope.GetService<CommandHandlingContext>();
                _notificationHandlers.OnPrepareContext(command, context);

                var unitOfWork = context.CreateUnitOfWork<TIdentifier, TEventBase>();

                try
                {
                    await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
                }
                catch (ArgumentNullException ex) when (ex.ParamName == "source")
                {
                    throw new UnhandledCommandException(command);
                }

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

                        foreach (var @event in storedEvents)
                        {
                            await _mediator.Publish(@event, cancellationToken).ConfigureAwait(false);
                        }

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
    }
}
