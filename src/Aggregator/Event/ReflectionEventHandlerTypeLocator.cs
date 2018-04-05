using System;
using Aggregator.Internal;

namespace Aggregator.Event
{
    /// <summary>
    /// Reflection based implementation of <see cref="IEventHandlerTypeLocator"/>.
    /// </summary>
    public sealed class ReflectionEventHandlerTypeLocator : IEventHandlerTypeLocator
    {
        private static readonly ReflectionTypeLocator Locator = new ReflectionTypeLocator(typeof(IEventHandler<>));

        /// <summary>
        /// Get types that implement <see cref="IEventHandler{TEvent}"/> for the given event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>An array of types that implement <see cref="IEventHandler{TEvent}"/> for the given event type.</returns>
        public Type[] For<TEvent>() => Locator.For<TEvent>();
    }
}
