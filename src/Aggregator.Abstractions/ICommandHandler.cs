namespace Aggregator
{
    /// <summary>
    /// Interface for a class is able to handle a command of the given type.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<TCommand> : MediatR.IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }
}
