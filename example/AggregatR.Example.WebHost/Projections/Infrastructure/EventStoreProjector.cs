using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace AggregatR.Example.WebHost.Projections.Infrastructure
{
    internal sealed class EventStoreProjector : IDisposable
    {
        private readonly IEventStoreConnection _connection;
        private readonly Func<IEnumerable<IProjection>> _projectionsResolver;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private EventStoreAllCatchUpSubscription _subscription;

        public EventStoreProjector(string connectionString, Func<IEnumerable<IProjection>> projectionsResolver)
        {
            _connection = EventStoreConnection.Create(connectionString);
            _projectionsResolver = projectionsResolver;
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        public void Dispose()
        {
            _subscription.Stop();
            _connection.Close();
            _connection.Dispose();
        }

        public async Task Start()
        {
            await _connection.ConnectAsync().ConfigureAwait(false);

            var catchupSettings = new CatchUpSubscriptionSettings(100, 200, false, false);

            _subscription = _connection.SubscribeToAllFrom(
                lastCheckpoint: Position.Start,
                settings: catchupSettings,
                eventAppeared: (_, resolvedEvent) => ProcessEvent(resolvedEvent));
        }

        private void ProcessEvent(ResolvedEvent resolvedEvent)
        {
            if (!resolvedEvent.Event.IsJson) return;
            if (resolvedEvent.OriginalStreamId.StartsWith("$")) return;
            var eventJsonData = Encoding.UTF8.GetString(resolvedEvent.Event.Data);
            var @event = JsonConvert.DeserializeObject(eventJsonData, _jsonSerializerSettings);
            Task.WaitAll(_projectionsResolver().Select(x => x.Handle(@event)).ToArray());
        }
    }
}
