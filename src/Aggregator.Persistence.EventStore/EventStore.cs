using System;
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

        public IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context)
            => new EventStoreTransaction<TIdentifier, TEventBase>(_connection, _jsonSerializerSettings);
    }
}
