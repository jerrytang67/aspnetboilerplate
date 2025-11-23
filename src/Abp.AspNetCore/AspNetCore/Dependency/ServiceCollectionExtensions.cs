using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.AspNetCore.Dependency
{
    /// <summary>
    /// Extension methods for IServiceCollection
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Gets a singleton service from the service collection
        /// </summary>
        public static T GetSingletonService<T>(this IServiceCollection services)
        {
            var service = services.GetSingletonServiceOrNull<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Could not find singleton service: {typeof(T).AssemblyQualifiedName}");
            }

            return service;
        }

        /// <summary>
        /// Gets a singleton service from the service collection or returns null
        /// </summary>
        public static T GetSingletonServiceOrNull<T>(this IServiceCollection services)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T) && d.Lifetime == ServiceLifetime.Singleton);

            if (descriptor?.ImplementationInstance != null)
            {
                return (T)descriptor.ImplementationInstance;
            }

            if (descriptor?.ImplementationFactory != null)
            {
                return (T)descriptor.ImplementationFactory(null);
            }

            if (descriptor?.ImplementationType != null)
            {
                return (T)Activator.CreateInstance(descriptor.ImplementationType);
            }

            return default(T);
        }
    }
}
