using Abp.Dependency;
using Abp.Modules;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Abp.AspNetCore.Tests.ServiceRegistration
{
    /// <summary>
    /// Integration tests for the complete AddAbp() execution flow.
    /// Tests the two-phase initialization: ConfigureServices -> Build Container -> Initialize
    /// </summary>
    public class AbpServiceCollectionExtensions_AddAbp_IntegrationTests
    {
        [Fact]
        public void AddAbp_Should_Execute_Complete_Flow_Successfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });

            // Assert
            serviceProvider.ShouldNotBeNull();
            serviceProvider.ShouldBeOfType<AutofacServiceProvider>();
        }

        [Fact]
        public void AddAbp_Should_Register_Services_Before_Container_Build()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });

            // Assert - Services registered in ConfigureServices should be resolvable
            var testService = serviceProvider.GetService<ITestIntegrationService>();
            testService.ShouldNotBeNull();
            testService.ShouldBeOfType<TestIntegrationService>();
        }

        [Fact]
        public void AddAbp_Should_Build_Container_After_All_Registrations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });
            var iocManager = serviceProvider.GetService<IIocManager>() as IocManager;

            // Assert - Container should be built
            iocManager.ShouldNotBeNull();
            iocManager.IsContainerBuilt.ShouldBeTrue();
        }

        [Fact]
        public void AddAbp_Should_Return_AutofacServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });

            // Assert
            serviceProvider.ShouldBeOfType<AutofacServiceProvider>();
        }

        [Fact]
        public void AddAbp_Should_Make_All_Module_Services_Resolvable()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });

            // Assert - All services registered by modules should be resolvable
            var testService = serviceProvider.GetService<ITestIntegrationService>();
            testService.ShouldNotBeNull();

            var iocManager = serviceProvider.GetService<IIocManager>();
            iocManager.ShouldNotBeNull();

            var bootstrapper = serviceProvider.GetService<AbpBootstrapper>();
            bootstrapper.ShouldNotBeNull();
        }

        [Fact]
        public void AddAbp_Should_Call_ConfigureServices_Before_Building_Container()
        {
            // Arrange
            var services = new ServiceCollection();
            TestIntegrationModule.ConfigureServicesCalled = false;
            TestIntegrationModule.InitializeCalled = false;

            // Act
            var serviceProvider = services.AddAbp<TestIntegrationModule>(options =>
            {
                options.IocManager = new IocManager(); // Use fresh IocManager for each test
            });

            // Assert - ConfigureServices should have been called
            TestIntegrationModule.ConfigureServicesCalled.ShouldBeTrue();
            
            // Initialize should NOT have been called yet (it's called later in Configure())
            TestIntegrationModule.InitializeCalled.ShouldBeFalse();
        }

        /// <summary>
        /// Test module for integration tests
        /// </summary>
        private class TestIntegrationModule : AbpModule
        {
            public static bool ConfigureServicesCalled { get; set; }
            public static bool InitializeCalled { get; set; }

            public override void ConfigureServices()
            {
                ConfigureServicesCalled = true;
                IocManager.Register<ITestIntegrationService, TestIntegrationService>(DependencyLifeStyle.Singleton);
            }

            public override void Initialize()
            {
                InitializeCalled = true;
            }
        }

        private interface ITestIntegrationService { }
        private class TestIntegrationService : ITestIntegrationService { }
    }
}
