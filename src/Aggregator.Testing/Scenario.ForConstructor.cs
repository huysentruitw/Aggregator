using System;

namespace Aggregator.Testing
{
    public static partial class Scenario
    {
        public static ScenarioForConstructorContinuation<TAggregateRoot, TEventBase> ForConstructor<TAggregateRoot, TEventBase>(Func<TAggregateRoot> constructor)
            where TAggregateRoot : AggregateRoot<TEventBase>
            => new ScenarioForConstructorContinuation<TAggregateRoot, TEventBase>(constructor);

        public static ScenarioForConstructorContinuation<TAggregateRoot, object> ForConstructor<TAggregateRoot>(Func<TAggregateRoot> constructor)
            where TAggregateRoot : AggregateRoot
            => ForConstructor<TAggregateRoot, object>(constructor);
    }

    public sealed class ScenarioForConstructorContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
        private readonly TAggregateRoot _aggregateRoot;

        internal ScenarioForConstructorContinuation(Func<TAggregateRoot> constructor)
        {
            if (constructor == null) throw new ArgumentNullException(nameof(constructor));
            _aggregateRoot = constructor();
        }

        public ThenContinuation<TAggregateRoot, TEventBase> Then(params TEventBase[] expectedEvents)
            => new ThenContinuation<TAggregateRoot, TEventBase>(_aggregateRoot, () => { }, expectedEvents);
    }
}
