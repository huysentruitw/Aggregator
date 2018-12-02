using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregator.Microsoft.DependencyInjection
{
    /// <summary>
    /// Microsoft.Extensions.DependencyInjection implementation for <see cref="DI.IServiceScopeFactory"/>.
    /// </summary>
    public class ServiceScopeFactory : DI.IServiceScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new <see cref="ServiceScopeFactory"/> instance.
        /// </summary>
        /// <param name="serviceProvider">The parent service provider.</param>
        public ServiceScopeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Method that creates a new <see cref="ServiceScope"/> from which scoped services will be resolved.
        /// </summary>
        /// <returns>A disposable <see cref="IServiceScope"/> instance.</returns>
        public DI.IServiceScope CreateScope()
            => new ServiceScope(_serviceProvider.CreateScope());
    }
}
