using System;
using Abp.Dependency;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.AspNetCore.Dependency
{
    /// <summary>
    /// Service provider factory for integrating ABP with the modern HostBuilder pattern.
    /// This factory uses the pre-configured IocManager and builds the Autofac container
    /// when the HostBuilder requests it.
    /// </summary>
    public class AbpAutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        private readonly IocManager _iocManager;

        public AbpAutofacServiceProviderFactory(IocManager iocManager)
        {
            _iocManager = iocManager;
        }

        /// <summary>
        /// Returns the pre-configured ContainerBuilder from IocManager.
        /// This builder already has all ABP services and modules registered.
        /// </summary>
        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            // Return the existing builder that already has all registrations
            return _iocManager.Builder;
        }

        /// <summary>
        /// Builds the Autofac container and returns the service provider.
        /// This is called by HostBuilder after all configuration is complete.
        /// </summary>
        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            // Check if container is already built
            if (_iocManager.IocContainer == null)
            {
                // Build the container using IocManager
                _iocManager.BuildContainer();
            }
            
            // Return AutofacServiceProvider wrapping the built container
            return new AutofacServiceProvider(_iocManager.IocContainer);
        }
    }
}
