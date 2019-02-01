using System.Collections.Generic;

namespace AggregatR.Internal
{
    internal interface IAggregateRootInitializer<TEventBase>
    {
        void Initialize(IEnumerable<TEventBase> events);
    }
}
