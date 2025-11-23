using System;
using Abp.Dependency;
using Abp.Modules;
using Shouldly;
using Xunit;

namespace Abp.Tests.Startup
{
    /// <summary>
    /// Tests for AbpBootstrapper two-phase initialization lifecycle.
    /// Focuses on verifying that Initialize() requires the container to be built first.
    /// </summary>
    public class AbpBootstrapper_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Throw_Exception_When_Initialize_Called_Before_Container_Built()
        {
            // Arrange
            var bootstrapper = AbpBootstrapper.Create<TestBootstrapperModule>(options =>
            {
                options.IocManager = LocalIocManager;
            });

            // Act & Assert
            var exception = Assert.Throws<AbpInitializationException>(() =>
            {
                bootstrapper.Initialize();
            });

            exception.Message.ShouldContain("Container must be built");
            exception.Message.ShouldContain("before calling Initialize");
        }

        [Fact]
        public void Should_Verify_Container_Is_Built_Before_Initialize()
        {
            // Arrange
            var bootstrapper = AbpBootstrapper.Create<TestBootstrapperModule>(options =>
            {
                options.IocManager = LocalIocManager;
            });

            var iocManager = (IocManager)LocalIocManager;

            // Assert - Container not built yet
            iocManager.IsContainerBuilt.ShouldBeFalse();

            // Act - Build container
            iocManager.BuildContainer();

            // Assert - Container is now built
            iocManager.IsContainerBuilt.ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Build_Container_In_Initialize()
        {
            // This test verifies that Initialize() no longer builds the container itself.
            // The container must be built externally before calling Initialize().

            // Arrange
            var bootstrapper = AbpBootstrapper.Create<TestBootstrapperModule>(options =>
            {
                options.IocManager = LocalIocManager;
            });

            var iocManager = (IocManager)LocalIocManager;

            // Assert - Container not built
            iocManager.IsContainerBuilt.ShouldBeFalse();

            // Act & Assert - Initialize should throw because container is not built
            Should.Throw<AbpInitializationException>(() => bootstrapper.Initialize());

            // Container should still not be built after Initialize() throws
            iocManager.IsContainerBuilt.ShouldBeFalse();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    public class TestBootstrapperModule : AbpModule
    {
        public int ConfigureServicesCount { get; private set; }
        public int InitializeCount { get; private set; }
        public int PostInitializeCount { get; private set; }
        public int ShutdownCount { get; private set; }

        public override void ConfigureServices()
        {
            ConfigureServicesCount++;
        }

        public override void Initialize()
        {
            InitializeCount++;
        }

        public override void Shutdown()
        {
            ShutdownCount++;
        }
    }
}
