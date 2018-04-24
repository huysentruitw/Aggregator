using System;
using System.Threading.Tasks;

namespace Aggregator.Command
{
    /// <summary>
    /// Interface for a command processor implementation where the command base type is an <see cref="object"/>.
    /// </summary>
    public interface ICommandProcessor : ICommandProcessor<object>
    {
    }

    /// <summary>
    /// Interface for a command processor implementation.
    /// </summary>
    /// <typeparam name="TCommandBase">The command base type.</typeparam>
    public interface ICommandProcessor<TCommandBase>
    {
        /// <summary>
        /// Processes a single command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Process(TCommandBase command);
    }
}
