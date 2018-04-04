using System;
using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootInitializer<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        void Initialize(TIdentifier identifier, long expectedVersion, IEnumerable<TEventBase> events = null);
    }
}
