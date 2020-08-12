using System;

namespace Aggregator.Exceptions
{
    /// <summary>
    /// Base class for aggregate root exceptions.
    /// </summary>
    public abstract class AggregateRootException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRootException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        protected AggregateRootException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Base class for aggregate root exceptions with an identifier.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    public abstract class AggregateRootException<TIdentifier> : AggregateRootException
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRootException{TIdentifier}"/> class.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="message">The message.</param>
        protected AggregateRootException(TIdentifier identifier, string message)
            : base(BuildMessage(identifier, message))
        {
            Identifier = identifier;
        }

        /// <summary>
        /// Gets the identifier of the aggregate root that threw the exception.
        /// </summary>
        public TIdentifier Identifier { get; }

        private static string BuildMessage(TIdentifier identifier, string message)
            => $"Exception for aggregate root with identifier '{identifier}': {message}";
    }
}
