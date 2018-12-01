using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aggregator.DI
{
    public static class ReflectionTypeLocator
    {
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
