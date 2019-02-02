using System;
using System.Threading;

namespace Aggregator.Command
{
    /// <summary>
    /// Class holindg the notification handlers called by <see cref="CommandProcessor"/>.
    /// </summary>
    public class CommandProcessorNotificationHandlers : CommandProcessorNotificationHandlers<string, ICommand, IEvent>
    {
    }

    /// <summary>
    /// Class holding the notification handlers called by <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}"/>.
    /// </summary>
    public class CommandProcessorNotificationHandlers<TIdentifier, TCommandBase, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
        where TCommandBase : ICommand
        where TEventBase : IEvent
    {
        /// <summary>
        /// Handler invoked right after the command handling context is created during the <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}.Process(TCommandBase, CancellationToken)"/> call.
        /// </summary>
        public Action<TCommandBase, CommandHandlingContext> PrepareContext { get; set; } = null;

        /// <summary>
        /// Handler invoked right after the command handling context is created during the <see cref="CommandProcessor{TIdentifier, TCommandBase, TEventBase}.Process(TCommandBase, CancellationToken)"/> call.
        /// </summary>
        /// <param name="command">The command being processed.</param>
        /// <param name="context">The command handling context.</param>
        public virtual void OnPrepareContext(TCommandBase command, CommandHandlingContext context)
            => PrepareContext?.Invoke(command, context);

        /// <summary>
        /// Handler invoked right after events are retrieved from the unit-of-work and before storing/dispatching events.
        /// </summary>
        public Func<TEventBase, TCommandBase, CommandHandlingContext, TEventBase> EnrichEvent { get; set; } = null;

        /// <summary>
        /// Handler invoked right after events are retrieved from the unit-of-work and before storing/dispatching events.
        /// </summary>
        /// <param name="event">The event to enrich.</param>
        /// <param name="command">The command being processed.</param>
        /// <param name="context">The command handling context.</param>
        /// <returns>The enriched event.</returns>
        public TEventBase OnEnrichEvent(TEventBase @event, TCommandBase command, CommandHandlingContext context)
            => EnrichEvent != null ? EnrichEvent.Invoke(@event, command, context) : @event;
    }
}
