using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aggregator.Command;
using Aggregator.DI;

namespace Aggregator.Event
{
    /// <summary>
    /// This class is used by the <see cref="CommandProcessor{TIdentifier,TCommandBase,TEventBase}"/> for dispatching events.
    /// </summary>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public class EventDispatcher<TEventBase> : IEventDispatcher<TEventBase>
    {
        private readonly ConcurrentDictionary<Type, MethodInfo> _dispatchEventMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDispatcher{TEventBase}"/> class.
        /// </summary>
        /// <param name="serviceScopeFactory">The service scope factory.</param>
        public EventDispatcher(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        /// <inheritdoc/>
        public async Task Dispatch(TEventBase[] events, CancellationToken cancellationToken)
        {
            foreach (TEventBase @event in events ?? Array.Empty<TEventBase>())
            {
                MethodInfo dispatchEventMethod =
                    _dispatchEventMethodCache.GetOrAdd(@event.GetType(), type =>
                        GetType()
                            .GetMethod(nameof(DispatchEvent), BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.MakeGenericMethod(type))
                    ?? throw new InvalidOperationException($"Couldn't make generic {nameof(DispatchEvent)} method");

                await ((Task)dispatchEventMethod.Invoke(this, new object[] { @event, cancellationToken })).ConfigureAwait(false);
            }
        }

        private async Task DispatchEvent<TEvent>(TEvent @event, CancellationToken cancellationToken)
        {
            using (IServiceScope serviceScope = _serviceScopeFactory.CreateScope())
            {
                IEnumerable<IEventHandler<TEvent>> handlers = serviceScope.GetServices<IEventHandler<TEvent>>();
                foreach (IEventHandler<TEvent> handler in handlers ?? Enumerable.Empty<IEventHandler<TEvent>>())
                    await handler.Handle(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
