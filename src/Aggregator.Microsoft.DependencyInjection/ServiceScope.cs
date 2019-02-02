using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregator.Microsoft.DependencyInjection
{
    /// <summary>
    /// Microsoft.Extensions.DependencyInjection implementation of <see cref="DI.IServiceScope"/>.
    /// </summary>
    public class ServiceScope : DI.IServiceScope
    {
        private readonly IServiceScope _serviceScope;

        internal ServiceScope(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
        }

        /// <summary>
        /// Disposes the <see cref="ServiceScope"/> instance.
        /// </summary>
        public void Dispose()
        {
            _serviceScope.Dispose();
        }

        /// <summary>
        /// Resolves a service from the <see cref="ServiceScope"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to resolve.</param>
        /// <returns>An instance of <paramref name="serviceType"/>.</returns>
        public object GetService(Type serviceType)
            => _serviceScope.ServiceProvider.GetService(serviceType);
    }
}
