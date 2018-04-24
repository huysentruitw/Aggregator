namespace Aggregator.Internal
{
    internal interface IAggregateRootChangeTracker<TEventBase>
    {
        bool HasChanges { get; }

        TEventBase[] GetChanges();
    }
}
