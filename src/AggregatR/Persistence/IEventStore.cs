using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AggregatR.Command;

namespace AggregatR.Persistence
{
    /// <summary>
    /// Interface for an event store implementation.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public interface IEventStore<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Checks if an aggregate root with the given identifier exists.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <returns>True in case an aggregate root with the given identifier exists, false when not.</returns>
        Task<bool> Contains(TIdentifier identifier);

        /// <summary>
        /// Get events for a certain aggregate root.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <param name="minimumVersion">The minimum version.</param>
        /// <returns>The events starting at minimumVersion.</returns>
        Task<TEventBase[]> GetEvents(TIdentifier identifier, long minimumVersion = 0);

        /// <summary>
        /// Begins a disposable transaction.
        /// </summary>
        /// <param name="context">The command handling context.</param>
        /// <returns>A disposable event store transaction.</returns>
        IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context);
    }
}
