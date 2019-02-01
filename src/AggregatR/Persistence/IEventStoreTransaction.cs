using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AggregatR.Persistence
{
    /// <summary>
    /// Interface for an event store transaction.
    /// </summary>
    public interface IEventStoreTransaction<TIdentifier, TEventBase> : IDisposable
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Commit the transaction.
        /// </summary>
        Task Commit();

        /// <summary>
        /// Rollback the transaction.
        /// </summary>
        Task Rollback();

        /// <summary>
        /// Store events for a specific aggregate root.
        /// </summary>
        /// <param name="identifier">The aggregate identifier.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="events">The events to store for the aggregate root.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task StoreEvents(TIdentifier identifier, long expectedVersion, IEnumerable<TEventBase> events);
    }
}
