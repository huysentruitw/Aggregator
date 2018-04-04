using System;

namespace Aggregator.Command
{
    /// <summary>
    /// Interface for a command handler type locator.
    /// </summary>
    public interface ICommandHandlerTypeLocator
    {
        /// <summary>
        /// Get types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <returns>An array of types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.</returns>
        Type[] For<TCommand>();
    }
}
