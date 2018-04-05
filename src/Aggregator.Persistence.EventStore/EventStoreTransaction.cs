using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Aggregator.Persistence.EventStore
{
    public class EventStoreTransaction<TIdentifier, TEventBase> : IEventStoreTransaction<TIdentifier, TEventBase>
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly IEventStoreConnection _connection;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly Queue<EventStoreTransaction> _pendingTransactions = new Queue<EventStoreTransaction>();

        internal EventStoreTransaction(IEventStoreConnection connection, JsonSerializerSettings jsonSerializerSettings)
        {
            _connection = connection;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public void Dispose()
        {
            Rollback();
        }

        public async Task Commit()
        {
            while (_pendingTransactions.Any())
            {
                var transaction = _pendingTransactions.Dequeue();
                var result = await transaction.CommitAsync().ConfigureAwait(false);
            }
        }

        public Task Rollback()
        {
            while (_pendingTransactions.Any())
            {
                var transaction = _pendingTransactions.Dequeue();
                transaction.Rollback();
            }

            return Task.CompletedTask;
        }

        public async Task StoreEvents(TIdentifier identifier, long expectedVersion, IEnumerable<TEventBase> events)
        {
            var transaction = await _connection.StartTransactionAsync(identifier.ToString(), expectedVersion).ConfigureAwait(false);
            _pendingTransactions.Enqueue(transaction);

            var eventData = events
                .Select(@event => new EventData(
                    eventId: Guid.NewGuid(),
                    type: @event.GetType().FullName,
                    isJson: true,
                    data: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, typeof(object), _jsonSerializerSettings)),
                    metadata: Array.Empty<byte>()))
                .ToArray();

            await transaction.WriteAsync(eventData).ConfigureAwait(false);
        }
    }
}
