using System;
using AggregatR.DI;
using Autofac;

namespace AggregatR.Autofac
{
    /// <summary>
    /// Autofac implementation of <see cref="IServiceScope"/>.
    /// </summary>
    public sealed class ServiceScope : IServiceScope
    {
        private readonly ILifetimeScope _ownedLifetimeScope;

        internal ServiceScope(ILifetimeScope ownedLifetimeScope)
        {
            _ownedLifetimeScope = ownedLifetimeScope;
        }

        /// <summary>
        /// Disposes the <see cref="ServiceScope"/> instance.
        /// </summary>
        public void Dispose() => _ownedLifetimeScope.Dispose();

        /// <summary>
        /// Resolves a service from the <see cref="ServiceScope"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to resolve.</param>
        /// <returns>An instance of <paramref name="serviceType"/>.</returns>
        public object GetService(Type serviceType) => _ownedLifetimeScope.Resolve(serviceType);
    }
}
