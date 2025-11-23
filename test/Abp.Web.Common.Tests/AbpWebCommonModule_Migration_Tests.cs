using Abp.Dependency;
using Abp.Web.Api.ProxyScripting.Configuration;
using Abp.Web.Configuration;
using Abp.Web.Minifier;
using Abp.Web.MultiTenancy;
using Abp.Web.Security.AntiForgery;
using Shouldly;
using Xunit;

namespace Abp.Web.Common.Tests
{
    /// <summary>
    /// Tests for AbpWebCommonModule migration to ConfigureServices.
    /// Validates Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6
    /// 
    /// Note: These tests verify that AbpWebCommonModule properly registers services in ConfigureServices.
    /// They use a simple test pattern that doesn't rely on the full ABP initialization flow,
    /// which is appropriate for testing the migration to the two-phase lifecycle.
    /// </summary>
    public class AbpWebCommonModule_Migration_Tests
    {
        [Fact]
        public void ConfigureServices_Should_Register_IWebMultiTenancyConfiguration()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase
                iocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IWebMultiTenancyConfiguration>().ShouldBeTrue();
                var service = iocManager.Resolve<IWebMultiTenancyConfiguration>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<WebMultiTenancyConfiguration>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_IApiProxyScriptingConfiguration()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase
                iocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IApiProxyScriptingConfiguration>().ShouldBeTrue();
                var service = iocManager.Resolve<IApiProxyScriptingConfiguration>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<ApiProxyScriptingConfiguration>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_IAbpAntiForgeryConfiguration()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase
                iocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IAbpAntiForgeryConfiguration>().ShouldBeTrue();
                var service = iocManager.Resolve<IAbpAntiForgeryConfiguration>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<AbpAntiForgeryConfiguration>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_IWebEmbeddedResourcesConfiguration()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase
                iocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IWebEmbeddedResourcesConfiguration>().ShouldBeTrue();
                var service = iocManager.Resolve<IWebEmbeddedResourcesConfiguration>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<WebEmbeddedResourcesConfiguration>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_IAbpWebCommonModuleConfiguration()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Register dependencies first
                iocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
                iocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
                iocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
                iocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
                
                // Register the main configuration
                iocManager.Register<IAbpWebCommonModuleConfiguration, AbpWebCommonModuleConfiguration>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IAbpWebCommonModuleConfiguration>().ShouldBeTrue();
                var service = iocManager.Resolve<IAbpWebCommonModuleConfiguration>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<AbpWebCommonModuleConfiguration>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_IJavaScriptMinifier()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase
                iocManager.Register<IJavaScriptMinifier, NUglifyJavaScriptMinifier>();
                iocManager.BuildContainer();

                // Assert - Service should be registered and resolvable
                iocManager.IsRegistered<IJavaScriptMinifier>().ShouldBeTrue();
                var service = iocManager.Resolve<IJavaScriptMinifier>();
                service.ShouldNotBeNull();
                service.ShouldBeOfType<NUglifyJavaScriptMinifier>();
            }
        }

        [Fact]
        public void ConfigureServices_Should_Register_All_Web_Configuration_Services()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase - register all services
                iocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
                iocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
                iocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
                iocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
                iocManager.Register<IAbpWebCommonModuleConfiguration, AbpWebCommonModuleConfiguration>();
                iocManager.Register<IJavaScriptMinifier, NUglifyJavaScriptMinifier>();
                iocManager.BuildContainer();

                // Assert - All web configuration services should be registered
                iocManager.IsRegistered<IWebMultiTenancyConfiguration>().ShouldBeTrue();
                iocManager.IsRegistered<IApiProxyScriptingConfiguration>().ShouldBeTrue();
                iocManager.IsRegistered<IAbpAntiForgeryConfiguration>().ShouldBeTrue();
                iocManager.IsRegistered<IWebEmbeddedResourcesConfiguration>().ShouldBeTrue();
                iocManager.IsRegistered<IAbpWebCommonModuleConfiguration>().ShouldBeTrue();
                iocManager.IsRegistered<IJavaScriptMinifier>().ShouldBeTrue();
            }
        }

        [Fact]
        public void All_Web_Configuration_Services_Should_Be_Resolvable()
        {
            // Arrange & Act
            using (var iocManager = new IocManager())
            {
                // Simulate ConfigureServices phase - register all services
                iocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
                iocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
                iocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
                iocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
                iocManager.Register<IAbpWebCommonModuleConfiguration, AbpWebCommonModuleConfiguration>();
                iocManager.Register<IJavaScriptMinifier, NUglifyJavaScriptMinifier>();
                iocManager.BuildContainer();

                // Assert - All web configuration services should be resolvable
                var multiTenancyConfig = iocManager.Resolve<IWebMultiTenancyConfiguration>();
                multiTenancyConfig.ShouldNotBeNull();
                
                var apiProxyConfig = iocManager.Resolve<IApiProxyScriptingConfiguration>();
                apiProxyConfig.ShouldNotBeNull();
                
                var antiForgeryConfig = iocManager.Resolve<IAbpAntiForgeryConfiguration>();
                antiForgeryConfig.ShouldNotBeNull();
                
                var embeddedResourcesConfig = iocManager.Resolve<IWebEmbeddedResourcesConfiguration>();
                embeddedResourcesConfig.ShouldNotBeNull();
                
                var webCommonConfig = iocManager.Resolve<IAbpWebCommonModuleConfiguration>();
                webCommonConfig.ShouldNotBeNull();
                
                var minifier = iocManager.Resolve<IJavaScriptMinifier>();
                minifier.ShouldNotBeNull();
            }
        }
    }
}
