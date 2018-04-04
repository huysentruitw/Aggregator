using System;
using Aggregator.Command;

namespace Aggregator.Persistence
{
    /// <summary>
    /// Interface for an event store implementation.
    /// </summary>
    public interface IEventStore<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        /// <summary>
        /// Begins a disposable transaction.
        /// </summary>
        /// <param name="context">The command handling context.</param>
        /// <returns>A disposable event store transaction.</returns>
        IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context);
    }
}
