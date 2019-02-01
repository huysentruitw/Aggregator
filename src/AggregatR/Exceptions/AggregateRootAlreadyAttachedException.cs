using System;
using AggregatR.Internal;

namespace AggregatR.Exceptions
{
    /// <summary>
    /// Throw by <see cref="UnitOfWork{TIdentifier, TEventBase}"/> when a certain aggregate root gets attached for the second time.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    public class AggregateRootAlreadyAttachedException<TIdentifier> : AggregateRootException<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Constructs a new <see cref="AggregateRootAlreadyAttachedException{TIdentifier}"/> instance.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        public AggregateRootAlreadyAttachedException(TIdentifier identifier)
            : base(identifier, "Aggregate root already attached")
        {
        }
    }
}
