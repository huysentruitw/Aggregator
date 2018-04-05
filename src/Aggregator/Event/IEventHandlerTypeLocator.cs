using System;

namespace Aggregator.Event
{
    /// <summary>
    /// Interface for an event handler type locator.
    /// </summary>
    public interface IEventHandlerTypeLocator
    {
        /// <summary>
        /// Get types that implement <see cref="IEventHandler{TEvent}"/> for the given event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>An array of types that implement <see cref="IEventHandler{TEvent}"/> for the given event type.</returns>
        Type[] For<TEvent>();
    }
}
