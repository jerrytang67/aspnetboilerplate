using System;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    public class IocManager_ContainerBuildState_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void IsContainerBuilt_Should_Be_False_Initially()
        {
            // Arrange & Act
            var iocManager = new Abp.Dependency.IocManager();

            // Assert
            iocManager.IsContainerBuilt.ShouldBeFalse();
        }

        [Fact]
        public void IsContainerBuilt_Should_Be_True_After_BuildContainer()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();

            // Act
            iocManager.BuildContainer();

            // Assert
            iocManager.IsContainerBuilt.ShouldBeTrue();
        }

        [Fact]
        public void BuildContainer_Should_Throw_Exception_If_Called_Twice()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => iocManager.BuildContainer());
            exception.Message.ShouldContain("Container is already built");
            exception.Message.ShouldContain("cannot be rebuilt");
        }

        [Fact]
        public void Register_Generic_Should_Throw_Exception_After_Container_Build()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => 
                iocManager.Register<TestService>());
            
            exception.Message.ShouldContain("Cannot register services after container is built");
            exception.Message.ShouldContain("ConfigureServices()");
            exception.Message.ShouldContain(typeof(TestService).FullName);
        }

        [Fact]
        public void Register_Type_Should_Throw_Exception_After_Container_Build()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => 
                iocManager.Register(typeof(TestService)));
            
            exception.Message.ShouldContain("Cannot register services after container is built");
            exception.Message.ShouldContain("ConfigureServices()");
            exception.Message.ShouldContain(typeof(TestService).FullName);
        }

        [Fact]
        public void Register_Generic_With_Implementation_Should_Throw_Exception_After_Container_Build()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => 
                iocManager.Register<ITestService, TestService>());
            
            exception.Message.ShouldContain("Cannot register services after container is built");
            exception.Message.ShouldContain("ConfigureServices()");
            exception.Message.ShouldContain(typeof(ITestService).FullName);
            exception.Message.ShouldContain(typeof(TestService).FullName);
        }

        [Fact]
        public void Register_Type_With_Implementation_Should_Throw_Exception_After_Container_Build()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => 
                iocManager.Register(typeof(ITestService), typeof(TestService)));
            
            exception.Message.ShouldContain("Cannot register services after container is built");
            exception.Message.ShouldContain("ConfigureServices()");
            exception.Message.ShouldContain(typeof(ITestService).FullName);
            exception.Message.ShouldContain(typeof(TestService).FullName);
        }

        [Fact]
        public void RegisterAssemblyByConvention_Should_Throw_Exception_After_Container_Build()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act & Assert
            var exception = Should.Throw<AbpException>(() => 
                iocManager.RegisterAssemblyByConvention(typeof(IocManager_ContainerBuildState_Tests).Assembly));
            
            exception.Message.ShouldContain("Cannot register services after container is built");
            exception.Message.ShouldContain("ConfigureServices()");
        }

        [Fact]
        public void Registration_Before_Build_Should_Work()
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();

            // Act - should not throw
            iocManager.Register<ITestService, TestService>();
            iocManager.BuildContainer();

            // Assert
            var service = iocManager.Resolve<ITestService>();
            service.ShouldNotBeNull();
            service.ShouldBeOfType<TestService>();
        }

        // Test helper classes
        public interface ITestService
        {
        }

        public class TestService : ITestService
        {
        }
    }
}
