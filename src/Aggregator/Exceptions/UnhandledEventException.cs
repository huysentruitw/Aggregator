using System;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Thrown when the aggregate needs to handle an event that doesn't have a handler registered.
    /// </summary>
    /// <typeparam name="TIdentifier"></typeparam>
    public class UnhandledEventException<TIdentifier> : AggregateRootException<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>
    {
        internal UnhandledEventException(TIdentifier identifier, Type eventType)
            : base(identifier, $"Unhandled event {eventType.Name}")
        {
        }
    }
}
