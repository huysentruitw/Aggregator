using System;
using System.Linq;
using Aggregator.Internal;

namespace Aggregator.Testing
{
    public static partial class Scenario
    {
        public static ScenarioForCommandContinuation<TAggregateRoot, TEventBase> ForCommand<TAggregateRoot, TEventBase>(Func<TAggregateRoot> constructor)
            where TAggregateRoot : AggregateRoot<TEventBase>
            => new ScenarioForCommandContinuation<TAggregateRoot, TEventBase>(constructor);

        public static ScenarioForCommandContinuation<TAggregateRoot, object> ForCommand<TAggregateRoot>(Func<TAggregateRoot> constructor)
            where TAggregateRoot : AggregateRoot
            => ForCommand<TAggregateRoot, object>(constructor);
    }

    public sealed class ScenarioForCommandContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
        private readonly TAggregateRoot _aggregateRoot;

        internal ScenarioForCommandContinuation(Func<TAggregateRoot> constructor)
        {
            if (constructor == null) throw new ArgumentNullException(nameof(constructor));
            _aggregateRoot = constructor();
        }

        public GivenContinuation<TAggregateRoot, TEventBase> Given(params TEventBase[] initialEvents)
        {
            if (initialEvents == null) throw new ArgumentNullException(nameof(initialEvents));
            if (!initialEvents.Any()) throw new ArgumentException("Array should not be empty", nameof(initialEvents));
            ((IAggregateRootInitializer<TEventBase>)_aggregateRoot).Initialize(initialEvents);
            return new GivenContinuation<TAggregateRoot, TEventBase>(_aggregateRoot);
        }
    }
}
