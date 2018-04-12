using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Aggregator.Exceptions;

namespace Aggregator.Internal
{
    internal sealed class UnitOfWork<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly ConcurrentDictionary<TIdentifier, AggregateRootEntity<TIdentifier, TEventBase>> _entities
            = new ConcurrentDictionary<TIdentifier, AggregateRootEntity<TIdentifier, TEventBase>>();

        public void Attach(AggregateRootEntity<TIdentifier, TEventBase> aggregateRootEntity)
        {
            if (aggregateRootEntity == null) throw new ArgumentNullException(nameof(aggregateRootEntity));
            if (!_entities.TryAdd(aggregateRootEntity.Identifier, aggregateRootEntity))
                throw new AggregateRootAlreadyAttachedException<TIdentifier>(aggregateRootEntity.Identifier);
        }

        public bool TryGet(TIdentifier identifier, out AggregateRootEntity<TIdentifier, TEventBase> aggregateRootEntity)
            => _entities.TryGetValue(identifier, out aggregateRootEntity);

        public bool HasChanges
            => _entities.Values.Any(x => x.HasChanges);

        public IEnumerable<AggregateRootEntity<TIdentifier, TEventBase>> GetChanges()
            => _entities.Values.Where(x => x.HasChanges);
    }
}
