using Abp.Dependency;
using Abp.Modules;
using Abp.TestBase;
using Shouldly;
using Xunit;

namespace Abp.Tests.Modules
{
    /// <summary>
    /// Integration tests for integration modules migration to ConfigureServices lifecycle.
    /// Tests Requirements 8.1, 8.2, 8.3, 8.4
    /// </summary>
    public class IntegrationModules_Integration_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void AutoMapper_Integration_Module_Should_Register_Services_In_ConfigureServices()
        {
            // This test verifies that AutoMapper-like integration modules register services correctly
            // Requirement 8.1
            
            // Arrange
            var module = new TestAutoMapperIntegrationModule();
            module.IocManager = LocalIocManager;
            
            // Act - ConfigureServices phase
            module.ConfigureServices();
            
            // Build container
            LocalIocManager.BuildContainer();
            
            // Assert - Services should be resolvable after container build
            var testService = LocalIocManager.Resolve<ITestAutoMapperIntegrationService>();
            testService.ShouldNotBeNull();
            testService.ShouldBeOfType<TestAutoMapperIntegrationService>();
        }

        [Fact]
        public void FluentValidation_Integration_Module_Should_Register_Services_In_ConfigureServices()
        {
            // This test verifies that FluentValidation-like integration modules register services correctly
            // Requirement 8.2
            
            // Arrange
            var module = new TestFluentValidationIntegrationModule();
            module.IocManager = LocalIocManager;
            
            // Act - ConfigureServices phase
            module.ConfigureServices();
            
            // Build container
            LocalIocManager.BuildContainer();
            
            // Assert - Services should be resolvable after container build
            var testService = LocalIocManager.Resolve<ITestFluentValidationIntegrationService>();
            testService.ShouldNotBeNull();
            testService.ShouldBeOfType<TestFluentValidationIntegrationService>();
        }

        [Fact]
        public void RedisCache_Integration_Module_Should_Register_Services_In_ConfigureServices()
        {
            // This test verifies that Redis cache-like integration modules register services correctly
            // Requirement 8.3
            
            // Arrange
            var module = new TestRedisCacheIntegrationModule();
            module.IocManager = LocalIocManager;
            
            // Act - ConfigureServices phase
            module.ConfigureServices();
            
            // Build container
            LocalIocManager.BuildContainer();
            
            // Assert - Services should be resolvable after container build
            var testService = LocalIocManager.Resolve<ITestRedisCacheIntegrationService>();
            testService.ShouldNotBeNull();
            testService.ShouldBeOfType<TestRedisCacheIntegrationService>();
        }

        [Fact]
        public void HtmlSanitizer_Integration_Module_Should_Register_Services_In_ConfigureServices()
        {
            // This test verifies that HTML sanitizer-like integration modules register services correctly
            // Requirement 8.4
            
            // Arrange
            var module = new TestHtmlSanitizerIntegrationModule();
            module.IocManager = LocalIocManager;
            
            // Act - ConfigureServices phase
            module.ConfigureServices();
            
            // Build container
            LocalIocManager.BuildContainer();
            
            // Assert - Services should be resolvable after container build
            var testService = LocalIocManager.Resolve<ITestHtmlSanitizerIntegrationService>();
            testService.ShouldNotBeNull();
            testService.ShouldBeOfType<TestHtmlSanitizerIntegrationService>();
        }

        [Fact]
        public void Integration_Modules_Should_Not_Register_Services_In_Initialize()
        {
            // This test verifies that integration modules cannot register services in Initialize
            
            // Arrange
            var module = new TestBadIntegrationModule();
            module.IocManager = LocalIocManager;
            
            // Act - ConfigureServices phase
            module.ConfigureServices();
            
            // Build container
            LocalIocManager.BuildContainer();
            
            // Act & Assert - Attempting to register in Initialize should throw
            var exception = Should.Throw<AbpException>(() =>
            {
                module.Initialize();
            });
            
            exception.Message.ShouldContain("Cannot register services after container is built");
        }

        // Test modules
        private class TestAutoMapperIntegrationModule : AbpModule
        {
            public override void ConfigureServices()
            {
                IocManager.Register<ITestAutoMapperIntegrationService, TestAutoMapperIntegrationService>();
            }
        }

        private class TestFluentValidationIntegrationModule : AbpModule
        {
            public override void ConfigureServices()
            {
                IocManager.Register<ITestFluentValidationIntegrationService, TestFluentValidationIntegrationService>();
            }
        }

        private class TestRedisCacheIntegrationModule : AbpModule
        {
            public override void ConfigureServices()
            {
                IocManager.Register<ITestRedisCacheIntegrationService, TestRedisCacheIntegrationService>();
            }
        }

        private class TestHtmlSanitizerIntegrationModule : AbpModule
        {
            public override void ConfigureServices()
            {
                IocManager.Register<ITestHtmlSanitizerIntegrationService, TestHtmlSanitizerIntegrationService>();
            }
        }

        private class TestBadIntegrationModule : AbpModule
        {
            public override void ConfigureServices()
            {
                // Correctly register in ConfigureServices
                IocManager.Register<ITestBadIntegrationService, TestBadIntegrationService>();
            }

            public override void Initialize()
            {
                // BAD: Try to register after container is built
                IocManager.Register<IAnotherTestService, AnotherTestService>();
            }
        }

        // Test service interfaces and implementations
        public interface ITestAutoMapperIntegrationService { }
        public class TestAutoMapperIntegrationService : ITestAutoMapperIntegrationService { }

        public interface ITestFluentValidationIntegrationService { }
        public class TestFluentValidationIntegrationService : ITestFluentValidationIntegrationService { }

        public interface ITestRedisCacheIntegrationService { }
        public class TestRedisCacheIntegrationService : ITestRedisCacheIntegrationService { }

        public interface ITestHtmlSanitizerIntegrationService { }
        public class TestHtmlSanitizerIntegrationService : ITestHtmlSanitizerIntegrationService { }

        public interface ITestBadIntegrationService { }
        public class TestBadIntegrationService : ITestBadIntegrationService { }

        public interface IAnotherTestService { }
        public class AnotherTestService : IAnotherTestService { }
    }
}
