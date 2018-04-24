using System;
using System.Threading.Tasks;

namespace Aggregator.Command
{
    /// <summary>
    /// Class holindg the notification handlers called by <see cref="CommandProcessor"/>.
    /// </summary>
    public class CommandProcessorNotificationHandlers : CommandProcessorNotificationHandlers<string, object, object>
    {
    }

    /// <summary>
    /// Class holding the notification handlers called by <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/>.
    /// </summary>
    public class CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Handler invoked right after the command handling context is created during the <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}.Process(TCommandBase)"/> call.
        /// </summary>
        public Func<TCommandBase, CommandHandlingContext, Task> PrepareContext { get; set; } = null;

        /// <summary>
        /// Handler invoked right after the command handling context is created during the <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}.Process(TCommandBase)"/> call.
        /// </summary>
        /// <param name="command">The command being processed.</param>
        /// <param name="context">The command handling context.</param>
        public virtual Task OnPrepareContext(TCommandBase command, CommandHandlingContext context)
            => PrepareContext != null ? PrepareContext?.Invoke(command, context) : Task.CompletedTask;
    }
}
