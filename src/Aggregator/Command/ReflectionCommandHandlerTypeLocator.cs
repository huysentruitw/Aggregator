using System;
using Aggregator.Internal;

namespace Aggregator.Command
{
    /// <summary>
    /// Reflection based implementation of <see cref="ICommandHandlerTypeLocator"/>.
    /// </summary>
    public sealed class ReflectionCommandHandlerTypeLocator : ICommandHandlerTypeLocator
    {
        private static readonly ReflectionTypeLocator Locator = new ReflectionTypeLocator(typeof(ICommandHandler<>));

        /// <summary>
        /// Get types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.
        /// </summary>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <returns>An array of types that implement <see cref="ICommandHandler{TCommand}"/> for the given command type.</returns>
        public Type[] For<TCommand>() => Locator.For<TCommand>();
    }
}
