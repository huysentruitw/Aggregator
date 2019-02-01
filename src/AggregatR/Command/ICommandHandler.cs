using System.Threading.Tasks;

namespace AggregatR.Command
{
    /// <summary>
    /// Interface for a class is able to handle a command of the given type.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<TCommand>
    {
        /// <summary>
        /// Called when a command of the given type needs to be handled.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task Handle(TCommand command);
    }
}
