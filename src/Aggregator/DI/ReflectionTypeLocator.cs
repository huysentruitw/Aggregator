using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aggregator.DI
{
    /// <summary>
    /// Static class used for locating implementation of generic interfaces in the given assemblies.
    /// </summary>
    public static class ReflectionTypeLocator
    {
        /// <summary>
        /// Locates all implementation of the given generic interface.
        /// </summary>
        /// <param name="genericInterfaceType">The generic interface.</param>
        /// <param name="assemblies">The assemblies to search in.</param>
        /// <returns>List of all found implementations that implement the given <paramref name="genericInterfaceType"/>.</returns>
        public static IEnumerable<Type> Locate(Type genericInterfaceType, params Assembly[] assemblies)
            => assemblies.SelectMany(assembly => FindImplementationsInAssembly(genericInterfaceType, assembly));

        private static IEnumerable<Type> FindImplementationsInAssembly(Type genericInterfaceType, Assembly assembly)
            => assembly.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && ImplementsGenericInterface(genericInterfaceType, x));

        private static bool ImplementsGenericInterface(Type genericInterfaceType, Type type)
            => type.GetInterfaces()
                .Any(x => x.IsGenericType && genericInterfaceType.Equals(x.GetGenericTypeDefinition()));
    }
}
