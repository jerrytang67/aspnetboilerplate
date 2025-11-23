using Abp.Application.Features;
using Abp.Auditing;
using Abp.BackgroundJobs;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Dependency.Installers;
using Abp.Domain.Uow;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Localization;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.Notifications;
using Abp.Reflection;
using Abp.Resources.Embedded;
using Abp.Runtime.Caching.Configuration;
using Abp.Runtime.Validation;
using Abp.Webhooks;
using Autofac;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    /// <summary>
    /// Tests for AbpCoreModule migration - validates that the old Autofac Module pattern
    /// has been replaced with direct registration and EarlyInitialize() + RegisterInstance().
    /// Validates Requirements: 5.1, 5.2
    /// </summary>
    public class AbpCoreModule_Migration_Tests
    {
        [Fact]
        public void RegisterCoreServices_Should_Be_Safe_To_Call_Early()
        {
            // Arrange
            var iocManager = new IocManager();

            // Act - Should not throw, even if called multiple times
            AbpCoreModule.RegisterCoreServices(iocManager.Builder);
            AbpCoreModule.RegisterCoreServices(iocManager.Builder); // Called twice

            // Verify container is not built yet
            iocManager.IsContainerBuilt.ShouldBeFalse();

            // Should be able to build container after
            iocManager.BuildContainer();
            iocManager.IsContainerBuilt.ShouldBeTrue();

            iocManager.Dispose();
        }

        [Fact]
        public void RegisterCoreServices_Should_Register_Infrastructure_Services()
        {
            // Arrange
            var iocManager = new IocManager();

            // Act
            AbpCoreModule.RegisterCoreServices(iocManager.Builder);
            iocManager.BuildContainer();

            // Assert - Core infrastructure services should be registered
            iocManager.IsRegistered<ITypeFinder>().ShouldBeTrue();
            iocManager.IsRegistered<IAssemblyFinder>().ShouldBeTrue();
            iocManager.IsRegistered<ILocalizationManager>().ShouldBeTrue();
            iocManager.IsRegistered<IDynamicEntityPropertyDefinitionContext>().ShouldBeTrue();

            iocManager.Dispose();
        }

        [Fact]
        public void Configuration_Objects_Should_Be_Registered_Via_EarlyInitialize_And_RegisterInstance()
        {
            // This test validates that configuration objects (like ILocalizationConfiguration)
            // are NOT registered by AbpCoreModule, but by CreateAndInitializeModuleManager
            // using EarlyInitialize() + RegisterInstance()

            // Arrange
            var iocManager = new IocManager();
            var configuration = new AbpStartupConfiguration(iocManager);

            // Act - Call EarlyInitialize to create configuration objects
            configuration.EarlyInitialize();

            // RegisterCoreServices should not register configuration objects
            AbpCoreModule.RegisterCoreServices(iocManager.Builder);

            // Register configuration instances manually (simulating CreateAndInitializeModuleManager)
            iocManager.Builder.RegisterInstance(configuration)
                .As<IAbpStartupConfiguration>()
                .As<AbpStartupConfiguration>()
                .SingleInstance()
                .ExternallyOwned();

            iocManager.Builder.RegisterInstance(configuration.Localization)
                .As<ILocalizationConfiguration>()
                .As<LocalizationConfiguration>()
                .SingleInstance()
                .ExternallyOwned();

            iocManager.Builder.RegisterInstance(configuration.UnitOfWork)
                .As<IUnitOfWorkDefaultOptions>()
                .As<UnitOfWorkDefaultOptions>()
                .SingleInstance()
                .ExternallyOwned();

            iocManager.BuildContainer();

            // Assert - Configuration objects should be resolvable and be the same instances
            var resolvedConfig = iocManager.Resolve<IAbpStartupConfiguration>();
            resolvedConfig.ShouldNotBeNull();
            resolvedConfig.ShouldBeSameAs(configuration); // Should be the exact same instance

            var localizationConfig = iocManager.Resolve<ILocalizationConfiguration>();
            localizationConfig.ShouldNotBeNull();
            localizationConfig.ShouldBeSameAs(configuration.Localization);

            var unitOfWorkConfig = iocManager.Resolve<IUnitOfWorkDefaultOptions>();
            unitOfWorkConfig.ShouldNotBeNull();
            unitOfWorkConfig.ShouldBeSameAs(configuration.UnitOfWork);

            iocManager.Dispose();
        }

        [Fact]
        public void Configuration_Changes_During_ConfigureServices_Should_Be_Preserved()
        {
            // This test validates that configuration changes made during ConfigureServices
            // are preserved because we register instances (not types)

            // Arrange
            var iocManager = new IocManager();
            var configuration = new AbpStartupConfiguration(iocManager);
            configuration.EarlyInitialize();

            // Simulate ConfigureServices - modify configuration BEFORE registering instance
            configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    "TestSource",
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AbpCoreModule_Migration_Tests).Assembly,
                        "Abp.Tests.TestLocalization"
                    )));

            // Act - Register the modified instance
            AbpCoreModule.RegisterCoreServices(iocManager.Builder);

            iocManager.Builder.RegisterInstance(configuration.Localization)
                .As<ILocalizationConfiguration>()
                .SingleInstance()
                .ExternallyOwned();

            iocManager.BuildContainer();

            // Assert - Changes should be preserved
            var resolvedConfig = iocManager.Resolve<ILocalizationConfiguration>();
            resolvedConfig.Sources.Count.ShouldBe(1);
            resolvedConfig.Sources[0].Name.ShouldBe("TestSource");

            iocManager.Dispose();
        }
    }
}
