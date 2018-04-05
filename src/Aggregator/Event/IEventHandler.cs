using System.Threading.Tasks;

namespace Aggregator.Event
{
    /// <summary>
    /// Interface for a class is able to handle an event of the given type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    public interface IEventHandler<TEvent>
    {
        /// <summary>
        /// Called when an event of the given type needs to be handled.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Handle(TEvent @event);
    }
}
