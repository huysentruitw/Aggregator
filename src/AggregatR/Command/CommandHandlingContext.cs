using System.Collections.Generic;

namespace AggregatR.Command
{
    /// <summary>
    /// This class keeps track of the command handling context for the lifetime of a single command.
    /// </summary>
    public sealed class CommandHandlingContext
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Sets a context property.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        public void Set<T>(string key, T value)
            => _properties[key] = value;

        /// <summary>
        /// Gets a context property.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="key">The property key.</param>
        /// <returns>The property value or default in case the property was not found.</returns>
        public T Get<T>(string key)
            => _properties.TryGetValue(key, out var value) ? (T)value : default(T);
    }
}
