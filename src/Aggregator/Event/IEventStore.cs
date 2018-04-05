using System;
using Aggregator.Command;

namespace Aggregator.Event
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
        /// Begins a disposable transaction.
        /// </summary>
        /// <param name="context">The command handling context.</param>
        /// <returns>A disposable event store transaction.</returns>
        IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context);
    }
}
