using System;
using System.Linq;
using Aggregator.Internal;
using Newtonsoft.Json;

namespace Aggregator.Testing
{
    public sealed class ThenContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new ExceptionContractResolver()
        };

        private readonly TAggregateRoot _aggregateRoot;
        private readonly Action _action;
        private readonly TEventBase[] _expectedEvents;

        internal ThenContinuation(TAggregateRoot aggregateRoot, Action action, TEventBase[] expectedEvents)
        {
            _aggregateRoot = aggregateRoot;
            _action = action;
            _expectedEvents = expectedEvents;
        }

        public void Assert()
        {
            _action();

            var aggregateRoot = (IAggregateRootChangeTracker<TEventBase>)_aggregateRoot;
            var events = aggregateRoot.GetChanges();

            if (aggregateRoot.HasChanges && !_expectedEvents.Any())
            {
                throw new AggregatorTestingException("Expecting 0 events while AggregateRoot.HasChanges is true");
            }
            else if (!aggregateRoot.HasChanges && _expectedEvents.Any())
            {
                throw new AggregatorTestingException("Expecting one or more events while AggregateRoot.HasChanges is false");
            }

            if (events.Length != _expectedEvents.Length)
            {
                throw new AggregatorTestingException($"Expecting {_expectedEvents.Length} event(s), but got {events.Length} event(s) instead");
            }

            for (var i = 0; i < events.Length; i++)
            {
                var eventType = events[i].GetType();
                var expectedEventType = _expectedEvents[i].GetType();

                if (expectedEventType != eventType)
                {
                    throw new AggregatorTestingException($"Expected event at index {i} to be of type {expectedEventType}, but got an event of type {eventType} instead");
                }

                string expectedJson = JsonConvert.SerializeObject(_expectedEvents[i], Formatting.Indented, JsonSettings);
                string json = JsonConvert.SerializeObject(events[i], Formatting.Indented, JsonSettings);
                if (json != expectedJson)
                {
                    throw new AggregatorTestingException($"Expected event:{Environment.NewLine}{expectedJson}{Environment.NewLine}but got event:{Environment.NewLine}{json} ");
                }
            }
        }
    }
}
