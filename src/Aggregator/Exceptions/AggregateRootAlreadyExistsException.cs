using System;
using Aggregator.Persistence;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Throw by <see cref="Repository{TIdentifier, TEventBase, TAggregateRoot}"/> when a certain aggregate root gets added but already exists.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    public class AggregateRootAlreadyExistsException<TIdentifier> : AggregateRootException<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRootAlreadyExistsException{TIdentifier}"/> class.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        public AggregateRootAlreadyExistsException(TIdentifier identifier)
            : base(identifier, "Aggregate root already attached")
        {
        }
    }
}
