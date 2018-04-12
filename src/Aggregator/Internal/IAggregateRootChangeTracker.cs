using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootChangeTracker<TEventBase>
    {
        bool HasChanges { get; }

        IEnumerable<TEventBase> GetChanges();
    }
}
