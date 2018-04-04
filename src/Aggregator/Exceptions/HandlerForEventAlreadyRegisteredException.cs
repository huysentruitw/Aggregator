using System;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Thrown when trying to register a handler for the same event more than once.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    public class HandlerForEventAlreadyRegisteredException<TIdentifier> : AggregateRootException<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>
    {
        internal HandlerForEventAlreadyRegisteredException(TIdentifier identifier, Type eventType)
            : base(identifier, $"Handler for event {eventType.Name} already registered")
        {
        }
    }
}
