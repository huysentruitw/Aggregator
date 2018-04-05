using System;
using Aggregator.Event;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac implementation for <see cref="IEventHandlingScopeFactory"/>.
    /// </summary>
    public class EventHandlingScopeFactory : IEventHandlingScopeFactory
    {
        private readonly IEventHandlerTypeLocator _eventHandlerTypeLocator;
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Constructs a new <see cref="EventHandlingScopeFactory"/> instance.
        /// </summary>
        /// <param name="eventHandlerTypeLocator">The event handler type locator.</param>
        /// <param name="lifetimeScope">The parent Autofac lifetime scope.</param>
        public EventHandlingScopeFactory(IEventHandlerTypeLocator eventHandlerTypeLocator, ILifetimeScope lifetimeScope)
        {
            _eventHandlerTypeLocator = eventHandlerTypeLocator ?? throw new ArgumentNullException(nameof(eventHandlerTypeLocator));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
        }

        /// <summary>
        /// Begin a scope for resolving event handlers that handle the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>The event handling scope.</returns>
        public IEventHandlingScope<TEvent> BeginScopeFor<TEvent>()
        {
            var handlerTypes = _eventHandlerTypeLocator.For<TEvent>() ?? Array.Empty<Type>();
            var innerScope = _lifetimeScope.BeginLifetimeScope(builder =>
            {
                Array.ForEach(handlerTypes, handlerType => builder.RegisterType(handlerType));
            });

            return new EventHandlingScope<TEvent>(innerScope, handlerTypes);
        }
    }
}
