namespace Aggregator.Internal
{
    internal interface IAggregateRootChangeTracker<TEventBase>
        where TEventBase : IEvent
    {
        bool HasChanges { get; }

        TEventBase[] GetChanges();
    }
}
