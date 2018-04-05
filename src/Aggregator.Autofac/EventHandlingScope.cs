using System;
using System.Linq;
using Aggregator.Event;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac implementation of <see cref="IEventHandlingScope{TEvent}"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public class EventHandlingScope<TEvent> : IEventHandlingScope<TEvent>
    {
        private readonly ILifetimeScope _ownedLifetimeScope;
        private readonly Type[] _handlerTypes;

        internal EventHandlingScope(ILifetimeScope ownedLifetimeScope, Type[] handlerTypes)
        {
            _ownedLifetimeScope = ownedLifetimeScope ?? throw new ArgumentNullException(nameof(ownedLifetimeScope));
            _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
        }

        /// <summary>
        /// Disposes this event handling scope.
        /// </summary>
        public void Dispose()
        {
            _ownedLifetimeScope.Dispose();
        }

        /// <summary>
        /// Gets all known handlers for the given event type.
        /// </summary>
        /// <returns>All known handlers for the given event type.</returns>
        public IEventHandler<TEvent>[] ResolveHandlers()
            => _handlerTypes
                .Select(type => (IEventHandler<TEvent>)_ownedLifetimeScope.Resolve(type))
                .ToArray();
    }
}
