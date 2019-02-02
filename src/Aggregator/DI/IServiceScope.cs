using System;

namespace Aggregator.DI
{
    /// <summary>
    /// Interface for the scope from which services can be resolved.
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// Method used to resolve all services. Multiple instances of a type will be resolved as <see cref="System.Collections.Generic.IEnumerable{T}"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to resolve.</param>
        /// <returns>An instance of type <paramref name="serviceType"/>.</returns>
        object GetService(Type serviceType);
    }
}
