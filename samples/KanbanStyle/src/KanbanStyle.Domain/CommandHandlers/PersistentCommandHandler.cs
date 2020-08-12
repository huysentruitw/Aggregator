using Aggregator;
using Aggregator.Persistence;

namespace KanbanStyle.Domain.CommandHandlers
{
    internal abstract class PersistentCommandHandler<TCommand, TAggregateRoot>
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
