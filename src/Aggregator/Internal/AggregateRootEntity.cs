using System;
using System.Collections.Generic;
using Aggregator.Internal;

namespace Aggregator.Internal
{
    internal class AggregateRootEntity<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        public TIdentifier Identifier { get; }

        public AggregateRoot<TEventBase> AggregateRoot { get; }

        public long ExpectedVersion { get; }

        public AggregateRootEntity(TIdentifier identifier, AggregateRoot<TEventBase> aggregateRoot, long expectedVersion)
        {
            Identifier = identifier;
            AggregateRoot = aggregateRoot;
            ExpectedVersion = expectedVersion;
        }

        public bool HasChanges
            => ((IAggregateRootChangeTracker<TEventBase>)AggregateRoot).HasChanges;

        public IEnumerable<TEventBase> GetChanges()
            => ((IAggregateRootChangeTracker<TEventBase>)AggregateRoot).GetChanges();
    }
}
