using System;

namespace Aggregator.Event
{
    /// <summary>
    /// Interface for a disposable event handling lifetime scope.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public interface IEventHandlingScope<TEvent> : IDisposable
    {
        /// <summary>
        /// Gets all known handlers for the given event type.
        /// </summary>
        /// <returns>All known handlers for the given event type.</returns>
        IEventHandler<TEvent>[] ResolveHandlers();
    }
}
