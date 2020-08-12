using System;
using Aggregator.Command;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/> when no command handler was found for the given command.
    /// </summary>
    public class UnhandledCommandException : Exception
    {
        internal UnhandledCommandException(object command)
            : base($"Unhandled command '{command.GetType().Name}'")
        {
            Command = command;
            CommandType = command.GetType();
        }

        /// <summary>
        /// Gets the unhandled command.
        /// </summary>
        public object Command { get; }

        /// <summary>
        /// Gets the type of the unhandled command.
        /// </summary>
        public Type CommandType { get; }
    }
}
