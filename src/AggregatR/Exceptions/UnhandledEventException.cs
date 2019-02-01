using System;

namespace AggregatR.Exceptions
{
    /// <summary>
    /// Thrown when the aggregate needs to handle an event that doesn't have a handler registered.
    /// </summary>
    public class UnhandledEventException : AggregateRootException
    {
        internal UnhandledEventException(Type eventType)
            : base($"Unhandled event {eventType.Name}")
        {
        }
    }
}
