using System;
using System.Linq;

namespace Aggregator.Testing
{
    public sealed class WhenContinuation<TAggregateRoot, TEventBase>
        where TAggregateRoot : AggregateRoot<TEventBase>
    {
        private readonly TAggregateRoot _aggregateRoot;
        private readonly Action _action;

        internal WhenContinuation(TAggregateRoot aggregateRoot, Action<TAggregateRoot> whenAction)
        {
            _aggregateRoot = aggregateRoot;
            _action = () => whenAction(_aggregateRoot);
        }

        public ThenContinuation<TAggregateRoot, TEventBase> Then(params TEventBase[] expectedEvents)
        {
            return new ThenContinuation<TAggregateRoot, TEventBase>(_aggregateRoot, _action, expectedEvents);
        }

        public ThrowsContinuation<TAggregateRoot, TEventBase, TException> Throws<TException>(TException exception)
            where TException : Exception
            => new ThrowsContinuation<TAggregateRoot, TEventBase, TException>(_aggregateRoot, _action, exception);

        public ThrowsContinuation<TAggregateRoot, TEventBase, TException> Throws<TException>()
            where TException : Exception, new()
            => Throws(new TException());
    }
}
