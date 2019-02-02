using System;
using Aggregator.DI;
using Autofac;

namespace Aggregator.Autofac
{
    /// <summary>
    /// Autofac implementation of <see cref="IServiceScopeFactory"/>.
    /// </summary>
    public sealed class ServiceScopeFactory : IServiceScopeFactory
    {
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Creates a new <see cref="ServiceScopeFactory"/> instance.
        /// </summary>
        /// <param name="lifetimeScope">The parent lifetime scope.</param>
        public ServiceScopeFactory(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
        }

        /// <summary>
        /// Method that creates a new <see cref="ServiceScope"/> from which scoped services will be resolved.
        /// </summary>
        /// <returns>A disposable <see cref="IServiceScope"/> instance.</returns>
        public IServiceScope CreateScope()
            => new ServiceScope(_lifetimeScope.BeginLifetimeScope());
    }
}
