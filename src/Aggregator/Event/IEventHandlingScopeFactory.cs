namespace Aggregator.Event
{
    /// <summary>
    /// Interface for the event handling scope factory.
    /// </summary>
    public interface IEventHandlingScopeFactory
    {
        /// <summary>
        /// Begin a scope for resolving event handlers that handle the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>The event handling scope.</returns>
        IEventHandlingScope<TEvent> BeginScopeFor<TEvent>();
    }
}
