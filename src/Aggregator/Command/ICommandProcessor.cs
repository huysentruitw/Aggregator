using System.Threading;
using System.Threading.Tasks;

namespace Aggregator.Command
{
    /// <summary>
    /// Interface for a command processor implementation where the commands derive from <see cref="object"/>.
    /// </summary>
    public interface ICommandProcessor : ICommandProcessor<object>
    {
    }

    /// <summary>
    /// Interface for a command processor implementation.
    /// </summary>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    public interface ICommandProcessor<in TCommandBase>
    {
        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="cancellationToken">A cancellation token that allows cancelling the process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Process(TCommandBase command, CancellationToken cancellationToken = default(CancellationToken));
    }
}
