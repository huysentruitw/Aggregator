namespace Aggregator.Internal
{
    internal interface IAggregateRootChangeTracker<out TEventBase>
    {
        bool HasChanges { get; }

        TEventBase[] GetChanges();
    }
}
