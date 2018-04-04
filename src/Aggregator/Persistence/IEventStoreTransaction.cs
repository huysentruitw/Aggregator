using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aggregator.Persistence
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
        void Commit();

        /// <summary>
        /// Rollback the transaction.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Store events for a specific aggregate root.
        /// </summary>
        /// <param name="identifier">The identifier of the aggregate root.</param>
        /// <param name="expectedRevision">The expected revision of the aggregate root.</param>
        /// <param name="events">The events to store for the aggregate root.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task StoreAggregateRootEvents(TIdentifier identifier, int expectedRevision, IEnumerable<TEventBase> events);
    }
}
