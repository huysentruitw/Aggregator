using System;

namespace Aggregator.Testing
{
    public sealed class GivenContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
        private readonly TAggregateRoot _aggregateRoot;

        internal GivenContinuation(TAggregateRoot aggregateRoot)
        {
            _aggregateRoot = aggregateRoot;
        }

        public WhenContinuation<TAggregateRoot, TEventBase> When(Action<TAggregateRoot> whenAction)
        {
            if (whenAction == null) throw new ArgumentNullException(nameof(whenAction));
            return new WhenContinuation<TAggregateRoot, TEventBase>(_aggregateRoot, whenAction);
        }
    }
}
