using Aggregator.Persistence;

namespace Aggregator.Example.Domain.CommandHandlers
{
    public abstract class PersistentCommandHandler<TCommand, TAggregateRoot>
        : SafeCommandHandler<TCommand>
        where TAggregateRoot : AggregateRoot, new()
    {
        protected readonly IRepository<TAggregateRoot> Repository;

        protected PersistentCommandHandler(IRepository<TAggregateRoot> repository)
        {
            Repository = repository;
        }
    }
}
