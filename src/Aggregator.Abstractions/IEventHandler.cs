using System.Threading;
using System.Threading.Tasks;

namespace Aggregator
{
    /// <summary>
    /// Interface for event handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event this handler is able to handle.</typeparam>
    public interface IEventHandler<in TEvent>
    {
        /// <summary>
        /// Called when an event of the given type needs to be handled.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="cancellationToken">A cancellation token that allows cancelling the process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Handle(TEvent @event, CancellationToken cancellationToken);
    }
}
