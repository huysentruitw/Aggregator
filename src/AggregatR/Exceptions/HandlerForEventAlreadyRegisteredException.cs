using System;

namespace AggregatR.Exceptions
{
    /// <summary>
    /// Thrown when trying to register a handler for the same event more than once.
    /// </summary>
    public class HandlerForEventAlreadyRegisteredException : AggregateRootException
    {
        internal HandlerForEventAlreadyRegisteredException(Type eventType)
            : base($"Handler for event {eventType.Name} already registered")
        {
        }
    }
}
