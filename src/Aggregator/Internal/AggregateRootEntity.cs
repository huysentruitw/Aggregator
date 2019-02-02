using System;

namespace Aggregator.Internal
{
    internal class AggregateRootEntity<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
        where TEventBase : IEvent
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

        public TEventBase[] GetChanges()
            => ((IAggregateRootChangeTracker<TEventBase>)AggregateRoot).GetChanges();
    }
}
