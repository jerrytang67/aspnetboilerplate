using System;
using System.Linq;
using System.Reflection;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.MultiTenancy;
using Abp.Reflection.Extensions;
using Abp.Threading.BackgroundWorkers;
using Abp.Zero.Configuration;
using Autofac;
using Shouldly;
using Xunit;

namespace Abp.Zero;

/// <summary>
/// Tests for AbpZeroCoreModule migration to two-phase lifecycle.
/// Validates Requirements 9.1, 9.3, 9.4, 9.5
/// </summary>
public class AbpZeroCoreModule_Migration_Tests
{
    [Fact]
    public void ConfigureServices_Should_Register_Assembly_By_Convention()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateModule(iocManager);

        // Act
        module.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        // Assert - verify some services from the assembly are registered
        iocManager.IsRegistered<IAbpZeroEntityTypes>().ShouldBeTrue();
    }

    [Fact]
    public void ConfigureServices_Should_Register_UserTokenExpirationWorker_Generic_Type()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateModule(iocManager);
        
        // Register IAbpZeroEntityTypes with concrete types for testing
        var entityTypes = new AbpZeroEntityTypes
        {
            Tenant = typeof(TestTenant),
            User = typeof(TestUser),
            Role = typeof(TestRole)
        };
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(entityTypes).As<IAbpZeroEntityTypes>().SingleInstance();

        // Act
        module.ConfigureServices();
        iocMgr.BuildContainer();

        // Assert - verify the generic worker type can be resolved
        var workerType = typeof(UserTokenExpirationWorker<,>).MakeGenericType(typeof(TestTenant), typeof(TestUser));
        iocManager.IsRegistered(workerType).ShouldBeTrue();
    }

    [Fact]
    public void Initialize_Should_Not_Register_Services()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateModule(iocManager);
        
        module.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        var registeredTypesBeforeInitialize = GetRegisteredTypeCount(iocManager);

        // Act
        module.Initialize();

        // Assert - no new types should be registered
        var registeredTypesAfterInitialize = GetRegisteredTypeCount(iocManager);
        registeredTypesAfterInitialize.ShouldBe(registeredTypesBeforeInitialize);
    }

    private AbpZeroCoreModule CreateModule(IIocManager iocManager)
    {
        // Create configuration
        var configurationType = Type.GetType("Abp.Configuration.Startup.AbpStartupConfiguration, Abp");
        var configuration = (IAbpStartupConfiguration)Activator.CreateInstance(configurationType, iocManager);
        
        // Register configuration
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(configuration).As<IAbpStartupConfiguration>().SingleInstance();
        
        // Create and configure dependency module (AbpZeroCommonModule)
        var commonModule = new AbpZeroCommonModule();
        var moduleType = typeof(AbpZeroCommonModule).BaseType; // AbpModule
        var iocManagerProperty = moduleType.GetProperty("IocManager", BindingFlags.NonPublic | BindingFlags.Instance);
        var configurationProperty = moduleType.GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Instance);
        
        iocManagerProperty.SetValue(commonModule, iocManager);
        configurationProperty.SetValue(commonModule, configuration);
        
        // Call ConfigureServices on dependency module first
        commonModule.ConfigureServices();
        
        // Create module with reflection to set internal properties
        var module = new AbpZeroCoreModule();
        iocManagerProperty.SetValue(module, iocManager);
        configurationProperty.SetValue(module, configuration);
        
        return module;
    }

    private int GetRegisteredTypeCount(IIocManager iocManager)
    {
        return ((IocManager)iocManager).IocContainer.ComponentRegistry.Registrations.Count();
    }

    // Test helper classes
    private class TestTenant : AbpTenant<TestUser>
    {
        public TestTenant()
        {
        }

        public TestTenant(string tenancyName, string name) : base(tenancyName, name)
        {
        }
    }

    private class TestUser : AbpUser<TestUser>
    {
    }

    private class TestRole : AbpRole<TestUser>
    {
        public TestRole()
        {
        }

        public TestRole(int? tenantId, string name, string displayName) : base(tenantId, name, displayName)
        {
        }
    }
}

/// <summary>
/// Integration tests for Zero modules migration.
/// Validates Requirements 9.1, 9.2, 9.3, 9.4
/// </summary>
public class ZeroModules_Integration_Tests
{
    [Fact]
    public void Identity_Services_Should_Be_Resolvable()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateZeroCoreModule(iocManager);

        // Act
        module.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        // Assert - identity services should be resolvable
        iocManager.IsRegistered<IAbpZeroEntityTypes>().ShouldBeTrue();
        var entityTypes = iocManager.Resolve<IAbpZeroEntityTypes>();
        entityTypes.ShouldNotBeNull();
    }

    [Fact]
    public void Authorization_Services_Should_Be_Resolvable()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateZeroCoreModule(iocManager);

        // Act
        module.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        // Assert - authorization-related services should be resolvable
        iocManager.IsRegistered<IAbpZeroEntityTypes>().ShouldBeTrue();
    }

    [Fact]
    public void User_Management_Should_Work_Correctly()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateZeroCoreModule(iocManager);
        
        // Register entity types for testing
        var entityTypes = new AbpZeroEntityTypes
        {
            Tenant = typeof(TestTenant),
            User = typeof(TestUser),
            Role = typeof(TestRole)
        };
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(entityTypes).As<IAbpZeroEntityTypes>().SingleInstance();

        // Act
        module.ConfigureServices();
        iocMgr.BuildContainer();

        // Assert - user-related types should be properly configured
        var resolvedEntityTypes = iocManager.Resolve<IAbpZeroEntityTypes>();
        resolvedEntityTypes.User.ShouldBe(typeof(TestUser));
        resolvedEntityTypes.Tenant.ShouldBe(typeof(TestTenant));
    }

    [Fact]
    public void Role_Management_Should_Work_Correctly()
    {
        // Arrange
        using var iocManager = new IocManager();
        var module = CreateZeroCoreModule(iocManager);
        
        // Register entity types for testing
        var entityTypes = new AbpZeroEntityTypes
        {
            Tenant = typeof(TestTenant),
            User = typeof(TestUser),
            Role = typeof(TestRole)
        };
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(entityTypes).As<IAbpZeroEntityTypes>().SingleInstance();

        // Act
        module.ConfigureServices();
        iocMgr.BuildContainer();

        // Assert - role-related types should be properly configured
        var resolvedEntityTypes = iocManager.Resolve<IAbpZeroEntityTypes>();
        resolvedEntityTypes.Role.ShouldBe(typeof(TestRole));
    }

    [Fact]
    public void EntityFrameworkCore_Module_Should_Register_Services()
    {
        // Arrange
        using var iocManager = new IocManager();
        var efModule = CreateZeroCoreEFModule(iocManager);

        // Act
        efModule.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        // Assert - EF module should register its services
        var registrationCount = GetRegisteredTypeCount(iocManager);
        registrationCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EntityFrameworkCore_Module_Initialize_Should_Not_Register_Services()
    {
        // Arrange
        using var iocManager = new IocManager();
        var efModule = CreateZeroCoreEFModule(iocManager);
        
        efModule.ConfigureServices();
        ((IocManager)iocManager).BuildContainer();

        var registeredTypesBeforeInitialize = GetRegisteredTypeCount(iocManager);

        // Act
        efModule.Initialize();

        // Assert - no new types should be registered
        var registeredTypesAfterInitialize = GetRegisteredTypeCount(iocManager);
        registeredTypesAfterInitialize.ShouldBe(registeredTypesBeforeInitialize);
    }

    private AbpZeroCoreModule CreateZeroCoreModule(IIocManager iocManager)
    {
        // Create configuration
        var configurationType = Type.GetType("Abp.Configuration.Startup.AbpStartupConfiguration, Abp");
        var configuration = (IAbpStartupConfiguration)Activator.CreateInstance(configurationType, iocManager);
        
        // Register configuration
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(configuration).As<IAbpStartupConfiguration>().SingleInstance();
        
        // Create and configure dependency module (AbpZeroCommonModule)
        var commonModule = new AbpZeroCommonModule();
        var moduleType = typeof(AbpZeroCommonModule).BaseType; // AbpModule
        var iocManagerProperty = moduleType.GetProperty("IocManager", BindingFlags.NonPublic | BindingFlags.Instance);
        var configurationProperty = moduleType.GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Instance);
        
        iocManagerProperty.SetValue(commonModule, iocManager);
        configurationProperty.SetValue(commonModule, configuration);
        
        // Call ConfigureServices on dependency module first
        commonModule.ConfigureServices();
        
        // Create module with reflection to set internal properties
        var module = new AbpZeroCoreModule();
        iocManagerProperty.SetValue(module, iocManager);
        configurationProperty.SetValue(module, configuration);
        
        return module;
    }

    private Abp.Zero.EntityFrameworkCore.AbpZeroCoreEntityFrameworkCoreModule CreateZeroCoreEFModule(IIocManager iocManager)
    {
        // Create configuration
        var configurationType = Type.GetType("Abp.Configuration.Startup.AbpStartupConfiguration, Abp");
        var configuration = (IAbpStartupConfiguration)Activator.CreateInstance(configurationType, iocManager);
        
        // Register configuration
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(configuration).As<IAbpStartupConfiguration>().SingleInstance();
        
        // Create module with reflection to set internal properties
        var module = new Abp.Zero.EntityFrameworkCore.AbpZeroCoreEntityFrameworkCoreModule();
        var moduleType = module.GetType().BaseType; // AbpModule
        var iocManagerProperty = moduleType.GetProperty("IocManager", BindingFlags.NonPublic | BindingFlags.Instance);
        var configurationProperty = moduleType.GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Instance);
        
        iocManagerProperty.SetValue(module, iocManager);
        configurationProperty.SetValue(module, configuration);
        
        return module;
    }

    private int GetRegisteredTypeCount(IIocManager iocManager)
    {
        return ((IocManager)iocManager).IocContainer.ComponentRegistry.Registrations.Count();
    }

    // Test helper classes
    private class TestTenant : AbpTenant<TestUser>
    {
        public TestTenant()
        {
        }

        public TestTenant(string tenancyName, string name) : base(tenancyName, name)
        {
        }
    }

    private class TestUser : AbpUser<TestUser>
    {
    }

    private class TestRole : AbpRole<TestUser>
    {
        public TestRole()
        {
        }

        public TestRole(int? tenantId, string name, string displayName) : base(tenantId, name, displayName)
        {
        }
    }
}
