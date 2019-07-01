using System;
using FluentAssertions;

namespace Aggregator.Testing
{
    public sealed class ThrowsContinuation<TAggregateRoot, TEventBase, TException>
        where TAggregateRoot : AggregateRoot<TEventBase>
        where TException : Exception
    {
        private readonly TAggregateRoot _aggregateRoot;
        private readonly Action _action;
        private readonly Exception _exception;

        internal ThrowsContinuation(TAggregateRoot aggregateRoot, Action action, Exception exception)
        {
            _aggregateRoot = aggregateRoot;
            _action = action;
            _exception = exception;
        }

        public void Assert()
        {
            _action.Should().Throw<TException>().Which.Should().BeEquivalentTo(_exception);
        }
    }
}
