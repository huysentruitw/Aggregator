using System;
using System.Linq;
using System.Threading.Tasks;
using AggregatR.Command;
using AggregatR.Exceptions;
using AggregatR.Internal;

namespace AggregatR.Persistence
{
    /// <summary>
    /// Implementation of <see cref="IRepository{TIdentifier, TEventBase, TAggregateRoot}"/> where the identifier type is <see cref="string"/> and the command/event base type is an <see cref="object"/>.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    public class Repository<TAggregateRoot> : Repository<string, object, TAggregateRoot>, IRepository<TAggregateRoot>
        where TAggregateRoot : AggregateRoot<object>, new()
    {
        /// <summary>
        /// Creates a new <see cref="Repository{TAggregateRoot}"/> instance.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="commandHandlingContext">The command handling context.</param>
        public Repository(IEventStore<string, object> eventStore, CommandHandlingContext commandHandlingContext)
            : base(eventStore, commandHandlingContext)
        {
        }
    }

    /// <summary>
    /// Implementation of <see cref="IRepository{TIdentifier, TEventBase, TAggregateRoot}"/>.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type.</typeparam>
    public class Repository<TIdentifier, TEventBase, TAggregateRoot> : IRepository<TIdentifier, TEventBase, TAggregateRoot>
        where TIdentifier : IEquatable<TIdentifier>
        where TAggregateRoot : AggregateRoot<TEventBase>, new()
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
        public async Task<TAggregateRoot> Get(TIdentifier identifier)
        {
            if (_unitOfWork.TryGet(identifier, out var aggregateRootEntity))
                return (TAggregateRoot)aggregateRootEntity.AggregateRoot;

            var events = await _eventStore.GetEvents(identifier).ConfigureAwait(false);
            if (events == null || !events.Any())
                throw new AggregateRootNotFoundException<TIdentifier>(identifier);

            var aggregateRoot = new TAggregateRoot();
            ((IAggregateRootInitializer<TEventBase>)aggregateRoot).Initialize(events);
            _unitOfWork.Attach(new AggregateRootEntity<TIdentifier, TEventBase>(identifier, aggregateRoot, events.Length));
            return aggregateRoot;
        }

        /// <summary>
        /// Adds a new aggregate root to the repository.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <param name="aggregateRoot">The aggregate root.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        /// <exception cref="AggregateRootAlreadyExistsException{TIdentifier}"></exception>
        public async Task Add(TIdentifier identifier, TAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null) throw new ArgumentNullException(nameof(aggregateRoot));
            if (await Contains(identifier).ConfigureAwait(false))
                throw new AggregateRootAlreadyExistsException<TIdentifier>(identifier);

            _unitOfWork.Attach(new AggregateRootEntity<TIdentifier, TEventBase>(identifier, aggregateRoot, 0));
        }
    }
}
