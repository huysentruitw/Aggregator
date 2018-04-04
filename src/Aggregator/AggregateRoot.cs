using System;
using System.Collections.Generic;
using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;

namespace Aggregator
{
    /// <summary>
    /// Base class for aggregate root entities.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public abstract class AggregateRoot<TIdentifier, TEventBase>
        : IAggregateRootInitializer<TIdentifier, TEventBase>
        , IAggregateRootChangeTracker<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly Dictionary<Type, Action<TEventBase>> _handlers = new Dictionary<Type, Action<TEventBase>>();
        private readonly List<TEventBase> _changes = new List<TEventBase>();
        private bool _isInitialized = false;

        /// <summary>
        /// The aggregate root identifier.
        /// </summary>
        public TIdentifier Identifier { get; private set; }

        /// <summary>
        /// The aggregate roots expected event version.
        /// </summary>
        public long ExpectedVersion { get; private set; }

        void IAggregateRootInitializer<TIdentifier, TEventBase>.Initialize(TIdentifier identifier, long expectedVersion, IEnumerable<TEventBase> events)
        {
            if (Equals(identifier, default(TIdentifier))) throw new ArgumentException("Default value not allowed", nameof(identifier));
            if (_isInitialized) throw new InvalidOperationException("Already initialized");

            Identifier = identifier;
            ExpectedVersion = expectedVersion;

            foreach (var @event in events ?? Enumerable.Empty<TEventBase>())
                Handle(@event);

            _isInitialized = true;
        }

        bool IAggregateRootChangeTracker<TIdentifier, TEventBase>.HasChanges
            => _changes.Any();

        IEnumerable<TEventBase> IAggregateRootChangeTracker<TIdentifier, TEventBase>.GetChanges()
        {
            GuardInitialized();
            return _changes.AsEnumerable();
        }

        /// <summary>
        /// Registers an event handler.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to bind the handler to.</typeparam>
        /// <param name="handler">The handler to invoke.</param>
        protected void Register<TEvent>(Action<TEvent> handler)
            where TEvent : TEventBase
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);
            if (_handlers.ContainsKey(eventType))
                throw new HandlerForEventAlreadyRegisteredException<TIdentifier>(Identifier, eventType);

            _handlers.Add(eventType, @event => handler((TEvent)@event));
        }

        /// <summary>
        /// Apply a new event to the aggregate root.
        /// </summary>
        /// <param name="event">The event to apply.</param>
        protected void Apply(TEventBase @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            GuardInitialized();
            Handle(@event);
            _changes.Add(@event);
        }

        private void Handle(TEventBase @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            var eventType = @event.GetType();
            if (!_handlers.TryGetValue(eventType, out var handler))
                throw new UnhandledEventException<TIdentifier>(Identifier, eventType);

            handler(@event);
        }

        private void GuardInitialized()
        {
            if (!_isInitialized) throw new InvalidOperationException("AggregateRoot not initialized");
        }
    }
}
