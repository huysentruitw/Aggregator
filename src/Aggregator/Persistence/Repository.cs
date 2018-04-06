using System;
using System.Linq;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.Exceptions;
using Aggregator.Internal;

namespace Aggregator.Persistence
{
    /// <summary>
    /// Implementation of <see cref="IRepository{TIdentifier, TEventBase, TAggregateRoot}"/>.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type.</typeparam>
    public class Repository<TIdentifier, TEventBase, TAggregateRoot> : IRepository<TIdentifier, TEventBase, TAggregateRoot>
        where TIdentifier : IEquatable<TIdentifier>
        where TAggregateRoot : AggregateRoot<TIdentifier, TEventBase>, new()
    {
        private readonly IEventStore<TIdentifier, TEventBase> _eventStore;
        private readonly UnitOfWork<TIdentifier, TEventBase> _unitOfWork;

        /// <summary>
        /// Creates a new <see cref="Repository{TIdentifier, TEventBase, TAggregateRoot}"/> instance.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="commandHandlingContext">The command handling context.</param>
        public Repository(IEventStore<TIdentifier, TEventBase> eventStore, CommandHandlingContext commandHandlingContext)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            if (commandHandlingContext == null) throw new ArgumentNullException(nameof(commandHandlingContext));
            _unitOfWork = commandHandlingContext.GetUnitOfWork<TIdentifier, TEventBase>();
            if (_unitOfWork == null) throw new ArgumentException("Failed to get unit of work from command handling context", nameof(commandHandlingContext));
        }

        /// <summary>
        /// Checks if an aggregate root with the given identifier exists.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>True in case an aggregate root with the given identifier exists, false when not.</returns>
        public async Task<bool> Contains(TIdentifier identifier)
        {
            if (_unitOfWork.TryGet(identifier, out _)) return true;
            return await _eventStore.Contains(identifier).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an aggregate root by its identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The aggregate root.</returns>
        /// <exception cref="AggregateRootNotFoundException{TIdentifier}"></exception>
        public Task<TAggregateRoot> Create(TIdentifier identifier)
        {
            var aggregateRoot = new TAggregateRoot();
            ((IAggregateRootInitializer<TIdentifier, TEventBase>)aggregateRoot).Initialize(identifier, 0);
            _unitOfWork.Attach(aggregateRoot);
            return Task.FromResult(aggregateRoot);
        }

        /// <summary>
        /// Creates a new aggregate root.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The new aggregate root.</returns>
        public async Task<TAggregateRoot> Get(TIdentifier identifier)
        {
            if (_unitOfWork.TryGet(identifier, out var attachedAggregateRoot))
                return (TAggregateRoot)attachedAggregateRoot;

            var events = await _eventStore.GetEvents(identifier).ConfigureAwait(false);
            if (events == null || !events.Any())
                throw new AggregateRootNotFoundException<TIdentifier>(identifier);

            var aggregateRoot = new TAggregateRoot();
            ((IAggregateRootInitializer<TIdentifier, TEventBase>)aggregateRoot).Initialize(identifier, events.Length, events);
            _unitOfWork.Attach(aggregateRoot);
            return aggregateRoot;
        }
    }
}
