using System;
using Abp.Dependency;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.AspNetCore.Dependency
{
    /// <summary>
    /// Helper class for Autofac registration with ASP.NET Core.
    /// </summary>
    public static class AutofacRegistrationHelper
    {
        /// <summary>
        /// Creates an IServiceProvider from an Autofac container and service collection.
        /// </summary>
        public static IServiceProvider CreateServiceProvider(IContainer container, IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // If container is null, we need to build one with the services
            if (container == null)
            {
                var builder = new ContainerBuilder();
                builder.Populate(services);
                container = builder.Build();
                return new AutofacServiceProvider(container);
            }

            // If container already exists, we can't update it (Autofac's Update method is obsolete)
            // In this case, the services should have been populated before the container was built
            // Just return an AutofacServiceProvider wrapping the existing container
            return new AutofacServiceProvider(container);
        }
    }
}
