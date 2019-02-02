namespace Aggregator.DI
{
    /// <summary>
    /// Interface for the service scope factory.
    /// </summary>
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Method that creates a new <see cref="IServiceScope"/> from which scoped services will be resolved.
        /// </summary>
        /// <returns>A disposable <see cref="IServiceScope"/> instance.</returns>
        IServiceScope CreateScope();
    }
}
