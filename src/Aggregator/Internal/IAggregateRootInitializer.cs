using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootInitializer<in TEventBase>
    {
        void Initialize(IEnumerable<TEventBase> events);
    }
}
