using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Aggregator.Internal
{
    internal sealed class UnitOfWork<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly ConcurrentDictionary<TIdentifier, IAggregateRootChangeTracker<TIdentifier, TEventBase>> _changes
            = new ConcurrentDictionary<TIdentifier, IAggregateRootChangeTracker<TIdentifier, TEventBase>>();

        public void Attach(AggregateRoot<TIdentifier, TEventBase> aggregateRoot)
        {
            if (aggregateRoot == null) throw new ArgumentNullException(nameof(aggregateRoot));
            if (!_changes.TryAdd(aggregateRoot.Identifier, aggregateRoot))
                throw new InvalidOperationException($"Aggregate root with identifier '{aggregateRoot.Identifier}' already attached");
        }

        public bool TryGet(TIdentifier identifier, out IAggregateRootChangeTracker<TIdentifier, TEventBase> aggregateRoot)
            => _changes.TryGetValue(identifier, out aggregateRoot);

        public bool HasChanges
            => _changes.Values.Any(x => x.HasChanges);

        public IEnumerable<IAggregateRootChangeTracker<TIdentifier, TEventBase>> GetChanges()
            => _changes.Values.Where(x => x.HasChanges);
    }
}
