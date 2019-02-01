using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AggregatR.Example.WebHost.Projections.Infrastructure
{
    internal abstract class ProjectionBase : IProjection
    {
        private readonly Dictionary<Type, Func<object, Task>> _handlers = new Dictionary<Type, Func<object, Task>>();

        public async Task Handle(object @event)
        {
            var eventType = @event.GetType();
            if (_handlers.TryGetValue(eventType, out var handler))
                await handler.Invoke(@event).ConfigureAwait(false);
        }

        protected void When<TEvent>(Func<TEvent, Task> handler)
            => _handlers.TryAdd(typeof(TEvent), @event => handler((TEvent)@event));

        protected void When<TEvent>(Action<TEvent> handler)
            => When<TEvent>(@event =>
            {
                handler(@event);
                return Task.CompletedTask;
            });
    }
}
