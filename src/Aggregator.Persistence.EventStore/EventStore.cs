using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aggregator.Command;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Aggregator.Persistence.EventStore
{
    public class EventStore<TIdentifier, TEventBase> : IEventStore<TIdentifier, TEventBase>, IDisposable
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly IEventStoreConnection _connection;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public EventStore(string connectionString)
        {
            _connection = EventStoreConnection.Create(connectionString);
            _connection.ConnectAsync().Wait();

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        public async Task<bool> Contains(TIdentifier identifier)
        {
            var result = await _connection.ReadEventAsync(identifier.ToString(), 0, false).ConfigureAwait(false);
            return result.Status == EventReadStatus.Success;
        }

        public Task<IEnumerable<TEventBase>> GetEvents(TIdentifier identifier, long minimumVersion = 1)
        {
            throw new NotImplementedException();
        }

        public IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context)
            => new EventStoreTransaction<TIdentifier, TEventBase>(_connection, _jsonSerializerSettings);
    }
}
