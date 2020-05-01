using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Aggregator.Persistence.EventStore
{
    /// <summary>
    /// Implementation of <see cref="IEventStoreTransaction{TIdentifier, TEventBase}"/> for eventstore.
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type.</typeparam>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public class EventStoreTransaction<TIdentifier, TEventBase> : IEventStoreTransaction<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly IEventStoreConnection _connection;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>> _createTransaction;
        private readonly Queue<IWrappedTransaction> _pendingTransactions = new Queue<IWrappedTransaction>();

        internal EventStoreTransaction(
            IEventStoreConnection connection,
            JsonSerializerSettings jsonSerializerSettings,
            Func<IEventStoreConnection, string, long, Task<IWrappedTransaction>> createTransaction = null)
        {
            _connection = connection;
            _jsonSerializerSettings = jsonSerializerSettings;
            _createTransaction = createTransaction ?? CreateTransaction;
        }

        /// <summary>
        /// Rolls back pending transactions.
        /// </summary>
        public void Dispose()
        {
            Rollback();
        }

        /// <summary>
        /// Commits pending transactions.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task Commit()
        {
            while (_pendingTransactions.Any())
            {
                using (var transaction = _pendingTransactions.Dequeue())
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Rolls back pending transactions.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public Task Rollback()
        {
            while (_pendingTransactions.Any())
            {
                using (var transaction = _pendingTransactions.Dequeue())
                {
                    transaction.Rollback();
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stores one or more events for a given aggregate root.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <param name="expectedVersion">The expected version of the event stream of the aggregate root.</param>
        /// <param name="events">The events to store.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task StoreEvents(TIdentifier identifier, long expectedVersion, IEnumerable<TEventBase> events, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transaction = await _createTransaction(_connection, identifier.ToString(), expectedVersion - 1).ConfigureAwait(false);
            _pendingTransactions.Enqueue(transaction);

            var eventData = events
                .Select(@event => new EventData(
                    eventId: Guid.NewGuid(),
                    type: @event.GetType().FullName,
                    isJson: true,
                    data: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, typeof(object), _jsonSerializerSettings)),
                    metadata: Array.Empty<byte>()))
                .ToArray();

            cancellationToken.ThrowIfCancellationRequested();
            await transaction.WriteAsync(eventData).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Task<IWrappedTransaction> CreateTransaction(IEventStoreConnection connection, string identifier, long expectedVersion)
        {
            var transaction = await connection.StartTransactionAsync(identifier, expectedVersion).ConfigureAwait(false);
            return new WrappedTransaction(transaction);
        }
    }

    internal interface IWrappedTransaction : IDisposable
    {
        Task WriteAsync(EventData[] eventData);

        Task CommitAsync();

        void Rollback();
    }

    [ExcludeFromCodeCoverage]
    internal class WrappedTransaction : IWrappedTransaction
    {
        private readonly EventStoreTransaction _transaction;

        public WrappedTransaction(EventStoreTransaction transaction)
        {
            _transaction = transaction;
        }

        public void Dispose() => _transaction.Dispose();

        public Task WriteAsync(EventData[] eventData) => _transaction.WriteAsync(eventData);

        public Task CommitAsync() => _transaction.CommitAsync();

        public void Rollback() => _transaction.Rollback();
    }
}
