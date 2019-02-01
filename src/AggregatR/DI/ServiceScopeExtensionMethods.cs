using System.Collections.Generic;

namespace AggregatR.DI
{
    internal static class ServiceScopeExtensionMethods
    {
        public static T GetService<T>(this IServiceScope serviceScope)
            => (T)serviceScope.GetService(typeof(T));

        public static IEnumerable<T> GetServices<T>(this IServiceScope serviceScope)
            => serviceScope.GetService<IEnumerable<T>>();
    }
}
