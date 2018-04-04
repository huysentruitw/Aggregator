using System;

namespace Aggregator.Command
{
    /// <summary>
    /// Interface for a disposable command handling lifetime scope.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandlingScope<TCommand> : IDisposable
    {
        /// <summary>
        /// Gets all known handlers for the given command type.
        /// </summary>
        /// <returns>All known handlers for the given command type.</returns>
        ICommandHandler<TCommand>[] ResolveHandlers();
    }
}
