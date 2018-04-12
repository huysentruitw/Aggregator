using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootInitializer<TEventBase>
    {
        void Initialize(IEnumerable<TEventBase> events);
    }
}
