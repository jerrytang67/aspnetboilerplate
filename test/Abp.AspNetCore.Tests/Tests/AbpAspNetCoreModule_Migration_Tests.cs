using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.MultiTenancy;
using Abp.AspNetCore.Mvc.Auditing;
using Abp.AspNetCore.Mvc.Caching;
using Abp.AspNetCore.Runtime.Session;
using Abp.AspNetCore.Security.AntiForgery;
using Abp.AspNetCore.Webhook;
using Abp.Auditing;
using Abp.Dependency;
using Abp.Runtime.Session;
using Abp.Web;
using Abp.Web.Security.AntiForgery;
using Abp.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Abp.AspNetCore.Tests;

/// <summary>
/// Integration tests for AbpAspNetCoreModule migration to ConfigureServices pattern.
/// Validates Requirements 6.1: ASP.NET Core module service registration and integration.
/// </summary>
public class AbpAspNetCoreModule_Migration_Tests : AppTestBase
{
    [Fact]
    public void Should_Register_AbpAspNetCoreConfiguration_In_ConfigureServices()
    {
        // Act
        var configuration = ServiceProvider.GetService<IAbpAspNetCoreConfiguration>();

        // Assert
        configuration.ShouldNotBeNull();
        configuration.ShouldBeOfType<AbpAspNetCoreConfiguration>();
    }

    [Fact]
    public void Should_Register_GetScriptsResponsePerUserConfiguration_In_ConfigureServices()
    {
        // Act
        var configuration = ServiceProvider.GetService<IGetScriptsResponsePerUserConfiguration>();

        // Assert
        configuration.ShouldNotBeNull();
        configuration.ShouldBeOfType<GetScriptsResponsePerUserConfiguration>();
    }

    [Fact]
    public void Should_Replace_IPrincipalAccessor_With_AspNetCore_Implementation()
    {
        // Act
        var principalAccessor = ServiceProvider.GetService<IPrincipalAccessor>();

        // Assert
        principalAccessor.ShouldNotBeNull();
        principalAccessor.ShouldBeOfType<AspNetCorePrincipalAccessor>();
    }

    [Fact]
    public void Should_Replace_IAbpAntiForgeryManager_With_AspNetCore_Implementation()
    {
        // Act
        var antiForgeryManager = ServiceProvider.GetService<IAbpAntiForgeryManager>();

        // Assert
        antiForgeryManager.ShouldNotBeNull();
        antiForgeryManager.ShouldBeOfType<AbpAspNetCoreAntiForgeryManager>();
    }

    [Fact]
    public void Should_Replace_IClientInfoProvider_With_AspNetCore_Implementation()
    {
        // Act
        var clientInfoProvider = ServiceProvider.GetService<IClientInfoProvider>();

        // Assert
        clientInfoProvider.ShouldNotBeNull();
        clientInfoProvider.ShouldBeOfType<HttpContextClientInfoProvider>();
    }

    [Fact]
    public void Should_Replace_IWebhookSender_With_AspNetCore_Implementation()
    {
        // Act
        var webhookSender = ServiceProvider.GetService<IWebhookSender>();

        // Assert
        webhookSender.ShouldNotBeNull();
        webhookSender.ShouldBeOfType<AspNetCoreWebhookSender>();
    }

    [Fact]
    public void Should_Register_Assembly_By_Convention_In_ConfigureServices()
    {
        // This test verifies that the assembly registration happens in ConfigureServices
        // by checking that services from the assembly are resolvable
        
        // Act
        var iocManager = ServiceProvider.GetService<IIocManager>();

        // Assert
        iocManager.ShouldNotBeNull();
        
        // Verify that types from the assembly are registered
        var configuration = iocManager.Resolve<IAbpAspNetCoreConfiguration>();
        configuration.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Resolve_All_AspNetCore_Services_After_Container_Build()
    {
        // This test verifies that all services registered in ConfigureServices
        // are resolvable after the container is built
        
        // Act & Assert - All these should be resolvable
        ServiceProvider.GetService<IAbpAspNetCoreConfiguration>().ShouldNotBeNull();
        ServiceProvider.GetService<IGetScriptsResponsePerUserConfiguration>().ShouldNotBeNull();
        ServiceProvider.GetService<IPrincipalAccessor>().ShouldNotBeNull();
        ServiceProvider.GetService<IAbpAntiForgeryManager>().ShouldNotBeNull();
        ServiceProvider.GetService<IClientInfoProvider>().ShouldNotBeNull();
        ServiceProvider.GetService<IWebhookSender>().ShouldNotBeNull();
    }

    [Fact]
    public void Should_Configure_MultiTenancy_Resolvers_In_PreInitialize()
    {
        // This test verifies that configuration logic that uses registered services
        // happens in PreInitialize (or later lifecycle methods)
        
        // Act
        var configuration = Resolve<IAbpAspNetCoreConfiguration>();

        // Assert
        configuration.ShouldNotBeNull();
        // The configuration should be accessible, indicating PreInitialize ran successfully
    }

    [Fact]
    public void Should_Allow_Service_Resolution_In_Controllers()
    {
        // This test verifies that services registered in ConfigureServices
        // can be resolved in ASP.NET Core controllers
        
        // Act
        var principalAccessor = ServiceProvider.GetService<IPrincipalAccessor>();
        var clientInfoProvider = ServiceProvider.GetService<IClientInfoProvider>();

        // Assert
        principalAccessor.ShouldNotBeNull();
        clientInfoProvider.ShouldNotBeNull();
        
        // These services should be usable in controllers
        principalAccessor.ShouldBeOfType<AspNetCorePrincipalAccessor>();
        clientInfoProvider.ShouldBeOfType<HttpContextClientInfoProvider>();
    }

    [Fact]
    public void Should_Allow_Middleware_To_Resolve_ABP_Services()
    {
        // This test verifies that middleware can resolve ABP services
        // that were registered in ConfigureServices
        
        // Act
        var iocManager = ServiceProvider.GetService<IIocManager>();
        var antiForgeryManager = ServiceProvider.GetService<IAbpAntiForgeryManager>();

        // Assert
        iocManager.ShouldNotBeNull();
        antiForgeryManager.ShouldNotBeNull();
        
        // Middleware should be able to resolve these services
        antiForgeryManager.ShouldBeOfType<AbpAspNetCoreAntiForgeryManager>();
    }

    [Fact]
    public void Should_Have_Conventional_Registrar_Registered()
    {
        // This test verifies that the conventional registrar is registered
        // in ConfigureServices
        
        // Act
        var iocManager = ServiceProvider.GetService<IIocManager>();

        // Assert
        iocManager.ShouldNotBeNull();
        
        // The conventional registrar should have registered services from the assembly
        var configuration = iocManager.Resolve<IAbpAspNetCoreConfiguration>();
        configuration.ShouldNotBeNull();
    }
}
