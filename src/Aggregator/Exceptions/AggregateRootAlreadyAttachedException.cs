using System;
using Aggregator.Internal;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Throw by <see cref="UnitOfWork{TIdentifier, TEventBase}"/> when a certain aggregate root gets attached for the second time.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    public class AggregateRootAlreadyAttachedException<TIdentifier> : AggregateRootException<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRootAlreadyAttachedException{TIdentifier}"/> class.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        public AggregateRootAlreadyAttachedException(TIdentifier identifier)
            : base(identifier, "Aggregate root already attached")
        {
        }
    }
}
