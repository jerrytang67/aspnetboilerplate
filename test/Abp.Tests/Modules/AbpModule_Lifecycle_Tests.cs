using System;
using System.Reflection;
using Abp.Modules;
using Shouldly;
using Xunit;

namespace Abp.Tests.Modules
{
    /// <summary>
    /// Tests for AbpModule lifecycle methods to verify the two-phase initialization model.
    /// Requirements: 1.1, 1.2, 1.3, 1.4, 1.5
    /// </summary>
    public class AbpModule_Lifecycle_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void ConfigureServices_Should_Have_Default_Empty_Implementation()
        {
            // Arrange
            var module = new TestModule();
            module.IocManager = LocalIocManager;

            // Act - should not throw
            module.ConfigureServices();

            // Assert - if we get here, the default implementation worked
            Assert.True(true);
        }

        [Fact]
        public void ConfigureServices_Should_Be_Virtual()
        {
            // Arrange & Act
            var method = typeof(AbpModule).GetMethod(nameof(AbpModule.ConfigureServices));

            // Assert
            method.ShouldNotBeNull();
            method.IsVirtual.ShouldBeTrue();
            method.GetParameters().Length.ShouldBe(0);
        }


        [Fact]
        public void Initialize_Should_Exist_With_Correct_Signature()
        {
            // Arrange & Act
            var method = typeof(AbpModule).GetMethod(nameof(AbpModule.Initialize));

            // Assert
            method.ShouldNotBeNull();
            method.IsVirtual.ShouldBeTrue();
            method.GetParameters().Length.ShouldBe(0);
            method.ReturnType.ShouldBe(typeof(void));
        }

        [Fact]
        public void Shutdown_Should_Exist_With_Correct_Signature()
        {
            // Arrange & Act
            var method = typeof(AbpModule).GetMethod(nameof(AbpModule.Shutdown));

            // Assert
            method.ShouldNotBeNull();
            method.IsVirtual.ShouldBeTrue();
            method.GetParameters().Length.ShouldBe(0);
            method.ReturnType.ShouldBe(typeof(void));
        }

        [Fact]
        public void All_Lifecycle_Methods_Should_Be_Callable()
        {
            // Arrange
            var module = new TestModule();
            module.IocManager = LocalIocManager;

            // Act & Assert - none should throw
            module.ConfigureServices();
            module.Initialize();
            module.Shutdown();
        }

        [Fact]
        public void Module_Can_Override_ConfigureServices()
        {
            // Arrange
            var module = new ModuleWithConfigureServices();
            module.IocManager = LocalIocManager;

            // Act
            module.ConfigureServices();

            // Assert
            module.ConfigureServicesCalled.ShouldBeTrue();
        }

        [Fact]
        public void Module_Can_Override_Initialize()
        {
            // Arrange
            var module = new ModuleWithInitialize();
            module.IocManager = LocalIocManager;

            // Act
            module.Initialize();

            // Assert
            module.InitializeCalled.ShouldBeTrue();
        }

        // Test modules
        private class TestModule : AbpModule
        {
        }

        private class ModuleWithConfigureServices : AbpModule
        {
            public bool ConfigureServicesCalled { get; private set; }

            public override void ConfigureServices()
            {
                ConfigureServicesCalled = true;
            }
        }

        private class ModuleWithInitialize : AbpModule
        {
            public bool InitializeCalled { get; private set; }

            public override void Initialize()
            {
                InitializeCalled = true;
            }
        }

        private class ModuleWithPostInitialize : AbpModule
        {
            public bool PostInitializeCalled { get; private set; }
        }
    }
}
