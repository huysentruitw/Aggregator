using System.Threading;
using System.Threading.Tasks;

namespace Aggregator.Command
{
    /// <summary>
    /// Interface for a command processor implementation where the command base type implements <see cref="ICommand"/>.
    /// </summary>
    public interface ICommandProcessor : ICommandProcessor<ICommand>
    {
    }

    /// <summary>
    /// Interface for a command processor implementation.
    /// </summary>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    public interface ICommandProcessor<TCommandBase>
        where TCommandBase : ICommand
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
