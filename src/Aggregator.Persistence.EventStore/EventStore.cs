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
    /// <summary>
    /// The eventstore implementation.
    /// </summary>
    public class EventStore : EventStore<string, object>
    {
        internal EventStore(IEventStoreConnection connection)
            : base(connection)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="EventStore"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        [ExcludeFromCodeCoverage]
        public EventStore(string connectionString)
            : base(connectionString)
        {
        }
    }

    /// <summary>
    /// The generic eventstore implementation.
    /// </summary>
    /// <typeparam name="TIdentifier">The </typeparam>
    /// <typeparam name="TEventBase"></typeparam>
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

        /// <summary>
        /// Constructs a new <see cref="EventStore"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        [ExcludeFromCodeCoverage]
        public EventStore(string connectionString)
            : this(Connect(connectionString))
        {
        }

        /// <summary>
        /// Frees resources and closes the underlying eventstore connection.
        /// </summary>
        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Checks if an event stream for a given aggregate root exists.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <returns>True if an event stream exists, false when not.</returns>
        public async Task<bool> Contains(TIdentifier identifier)
        {
            var result = await _connection.ReadEventAsync(identifier.ToString(), 0, false).ConfigureAwait(false);
            return result.Status == EventReadStatus.Success;
        }

        /// <summary>
        /// Gets the event stream for a given aggregate root.
        /// </summary>
        /// <param name="identifier">The aggregate root identifier.</param>
        /// <param name="minimumVersion">The minimum version of the event stream to retrieve.</param>
        /// <returns>The event stream.</returns>
        public async Task<TEventBase[]> GetEvents(TIdentifier identifier, long minimumVersion = 0)
        {
            var result = new List<TEventBase>();
            StreamEventsSlice currentSlice;
            var nextSliceStart = minimumVersion > 0 ? minimumVersion : StreamPosition.Start;
            do
            {
                currentSlice = await _connection.ReadStreamEventsForwardAsync(identifier.ToString(), nextSliceStart, 200, false).ConfigureAwait(false);
                foreach (var recordedEvent in currentSlice.Events)
                {
                    var eventJsonData = Encoding.UTF8.GetString(recordedEvent.Event.Data);
                    result.Add(JsonConvert.DeserializeObject<TEventBase>(eventJsonData, _jsonSerializerSettings));
                }

                nextSliceStart = currentSlice.NextEventNumber;
            }
            while (!currentSlice.IsEndOfStream);
            return result.ToArray();
        }

        /// <summary>
        /// Begins a new transaction.
        /// </summary>
        /// <param name="context">The command handling context.</param>
        /// <returns>The newly created transaction.</returns>
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
