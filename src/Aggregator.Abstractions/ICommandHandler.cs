using System.Threading;
using System.Threading.Tasks;

namespace Aggregator
{
    /// <summary>
    /// Interface for command handlers.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command this handler is able to handle.</typeparam>
    public interface ICommandHandler<in TCommand>
    {
        /// <summary>
        /// Called when a command of the given type needs to be handled.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">A cancellation token that allows cancelling the process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Handle(TCommand command, CancellationToken cancellationToken);
    }
}
