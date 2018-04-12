using System;
using System.Threading.Tasks;
using Aggregator.Exceptions;

namespace Aggregator.Persistence
{
    /// <summary>
    /// Interface for an aggregate root repository where the aggregate root identifier is a <see cref="string"/> and the command / event type is an <see cref="object"/>.
    /// </summary>
    /// <typeparam name="TAggregateRoot"></typeparam>
    public interface IRepository<TAggregateRoot> : IRepository<string, object, TAggregateRoot>
        where TAggregateRoot : AggregateRoot<object>, new()
    {
    }

    /// <summary>
    /// Interface for an aggregate root repository.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type.</typeparam>
    public interface IRepository<TIdentifier, TEventBase, TAggregateRoot>
        where TIdentifier : IEquatable<TIdentifier>
        where TAggregateRoot : AggregateRoot<TEventBase>, new()
    {
        /// <summary>
        /// Checks if an aggregate root with the given identifier exists.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>True in case an aggregate root with the given identifier exists, false when not.</returns>
        Task<bool> Contains(TIdentifier identifier);

        /// <summary>
        /// Gets an aggregate root by its identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The aggregate root.</returns>
        /// <exception cref="AggregateRootNotFoundException{TIdentifier}"></exception>
        Task<TAggregateRoot> Get(TIdentifier identifier);

        /// <summary>
        /// Adds a new aggregate root to the repository.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <param name="aggregateRoot">The aggregate root.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        /// <exception cref="AggregateRootAlreadyExistsException{TIdentifier}"></exception>
        Task Add(TIdentifier identifier, TAggregateRoot aggregateRoot);
    }
}
