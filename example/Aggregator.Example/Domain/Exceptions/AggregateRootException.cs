using System;

namespace Aggregator.Example.Domain.Exceptions
{
    public sealed class AggregateRootAlreadyRemovedException : Exception
    {
        public AggregateRootAlreadyRemovedException()
            : base("Aggregate root already removed")
        {
        }
    }
}
