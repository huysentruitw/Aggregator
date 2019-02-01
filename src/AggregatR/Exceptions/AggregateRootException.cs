using System;

namespace AggregatR.Exceptions
{
    /// <summary>
    /// Base class for aggregate root exceptions.
    /// </summary>
    public abstract class AggregateRootException : Exception
    {
        /// <summary>
        /// Constructs a <see cref="AggregateRootException"/> instance with a message.
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
        /// Constructs a <see cref="AggregateRootException{TIdentifier}"/> instance with a message.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="message">The message.</param>
        protected AggregateRootException(TIdentifier identifier, string message)
            : base(BuildMessage(identifier, message))
        {
            Identifier = identifier;
        }

        /// <summary>
        /// The identifier of the aggregate root that threw the exception.
        /// </summary>
        public TIdentifier Identifier { get; }

        private static string BuildMessage(TIdentifier identifier, string message)
            => $"Exception for aggregate root with identifier '{identifier}': {message}";
    }
}
