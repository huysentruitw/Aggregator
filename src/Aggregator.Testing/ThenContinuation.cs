using System;
using System.Linq;
using Aggregator.Internal;
using FluentAssertions;

namespace Aggregator.Testing
{
    public sealed class ThenContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
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

            aggregateRoot.HasChanges.Should().Be(_expectedEvents.Any());
            events.Should().HaveCount(_expectedEvents.Length);
            for (var i = 0; i < events.Length; i++)
            {
                events[i].Should().BeOfType(_expectedEvents[i].GetType());
                events[i].Should().BeEquivalentTo(_expectedEvents[i]);
            }
        }
    }
}
