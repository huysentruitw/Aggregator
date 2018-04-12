using System;
using Aggregator.Persistence;

namespace Aggregator.Example.Domain
{
    internal abstract class PersistentCommandHandler<TCommand, TAggregateRoot>
        : SafeCommandHandler<TCommand>
        where TAggregateRoot : AggregateRoot, new()
    {
        protected readonly IRepository<TAggregateRoot> Repository;

        public PersistentCommandHandler(IRepository<TAggregateRoot> repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
    }
}
