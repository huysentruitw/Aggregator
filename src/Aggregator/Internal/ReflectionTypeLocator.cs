using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aggregator.Internal
{
    internal sealed class ReflectionTypeLocator
    {
        private readonly Lazy<ILookup<Type, Type>> _cache;
        private readonly Type _genericInterfaceType;

        public ReflectionTypeLocator(Type genericInterfaceType)
        {
            _cache = new Lazy<ILookup<Type, Type>>(BuildCache);
            _genericInterfaceType = genericInterfaceType ?? throw new ArgumentNullException(nameof(genericInterfaceType));
        }

        public Type[] For<TArgument>() => _cache.Value[typeof(TArgument)]?.ToArray() ?? Array.Empty<Type>();

        private ILookup<Type, Type> BuildCache()
        {
            var implementations = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(FindImplementationsInAssembly)
                .ToArray();

            return implementations
                .SelectMany(x => x.GenericTypes, (x, genericType) => (ImplementationType: x.ImplementationType, GenericType: genericType))
                .ToLookup(x => x.GenericType, x => x.ImplementationType);
        }

        private IEnumerable<(Type ImplementationType, Type[] GenericTypes)> FindImplementationsInAssembly(Assembly assembly)
            => assembly.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract)
                .Select(x => (ImplementationType: x, GenericTypes: FindGenericTypes(x)))
                .Where(x => x.GenericTypes.Any());

        private Type[] FindGenericTypes(Type type)
            => type.GetInterfaces()
                .Where(x => x.IsGenericType && _genericInterfaceType.Equals(x.GetGenericTypeDefinition()))
                .Select(x => x.GenericTypeArguments[0])
                .ToArray();
    }
}
