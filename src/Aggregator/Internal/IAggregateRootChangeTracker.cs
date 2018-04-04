using System;
using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootChangeTracker<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        TIdentifier Identifier { get; }

        long ExpectedVersion { get; }

        bool HasChanges { get; }

        IEnumerable<TEventBase> GetChanges();
    }
}
