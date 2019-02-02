using System.Collections.Generic;

namespace Aggregator.Internal
{
    internal interface IAggregateRootInitializer<TEventBase>
        where TEventBase : IEvent
    {
        void Initialize(IEnumerable<TEventBase> events);
    }
}
