namespace Aggregator.Command
{
    /// <summary>
    /// Interface for the command handling scope factory.
    /// </summary>
    public interface ICommandHandlingScopeFactory
    {
        /// <summary>
        /// Begin a scope for resolving command handlers that handle the specified command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <param name="context">The command handling context.</param>
        /// <returns>The command handling scope.</returns>
        ICommandHandlingScope<TCommand> BeginScopeFor<TCommand>(CommandHandlingContext context);
    }
}
