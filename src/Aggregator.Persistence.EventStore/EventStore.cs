using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Aggregator.Command;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Aggregator.Persistence.EventStore
{
    public class EventStore : EventStore<string, object>
    {
        internal EventStore(IEventStoreConnection connection)
            : base(connection)
        {
        }

        [ExcludeFromCodeCoverage]
        public EventStore(string connectionString)
            : base(connectionString)
        {
        }
    }

    public class EventStore<TIdentifier, TEventBase> : IEventStore<TIdentifier, TEventBase>, IDisposable
        where TIdentifier : IEquatable<TIdentifier>
    {
        private readonly IEventStoreConnection _connection;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        internal EventStore(IEventStoreConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        [ExcludeFromCodeCoverage]
        public EventStore(string connectionString)
            : this(Connect(connectionString))
        {
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

        public async Task<TEventBase[]> GetEvents(TIdentifier identifier, long minimumVersion = 0)
        {
            var result = new List<TEventBase>();
            StreamEventsSlice currentSlice;
            var nextSliceStart = minimumVersion;
            do
            {
                currentSlice = await _connection.ReadStreamEventsForwardAsync(identifier.ToString(), minimumVersion, 200, false).ConfigureAwait(false);
                foreach (var recordedEvent in currentSlice.Events)
                {
                    var eventJsonData = Encoding.UTF8.GetString(recordedEvent.Event.Data);
                    result.Add(JsonConvert.DeserializeObject<TEventBase>(eventJsonData, _jsonSerializerSettings));
                }
            }
            while (!currentSlice.IsEndOfStream);
            return result.ToArray();
        }

        public IEventStoreTransaction<TIdentifier, TEventBase> BeginTransaction(CommandHandlingContext context)
            => new EventStoreTransaction<TIdentifier, TEventBase>(_connection, _jsonSerializerSettings);

        [ExcludeFromCodeCoverage]
        private static IEventStoreConnection Connect(string connectionString)
        {
            var connection = EventStoreConnection.Create(connectionString);
            connection.ConnectAsync().Wait();
            return connection;
        }
    }
}
