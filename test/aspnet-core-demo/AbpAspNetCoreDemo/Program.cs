using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Abp.AspNetCore;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AbpAspNetCoreDemo;

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = new CultureInfo("zh-Hans");
        CultureInfo.CurrentUICulture = new CultureInfo("zh-Hans");
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            // Use a custom service provider factory that integrates ABP with Autofac
            .UseServiceProviderFactory(new AbpAutofacServiceProviderFactoryAdapter<AbpAspNetCoreDemoModule>())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            });
    }
}

/// <summary>
/// Adapter that integrates ABP with Autofac in the modern HostBuilder pattern
/// </summary>
public class AbpAutofacServiceProviderFactoryAdapter<TStartupModule> : IServiceProviderFactory<ContainerBuilder>
    where TStartupModule : Abp.Modules.AbpModule
{
    private IServiceProviderFactory<ContainerBuilder> _abpFactory;

    public ContainerBuilder CreateBuilder(IServiceCollection services)
    {
        // Create ABP's factory with the actual service collection
        // This ensures ABP can populate the container with ASP.NET Core services
        _abpFactory = services.AddAbpWithoutBuildingContainer<TStartupModule>();
        
        // Return the configured ContainerBuilder
        return _abpFactory.CreateBuilder(services);
    }

    public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
    {
        // Build the container using ABP's factory
        return _abpFactory.CreateServiceProvider(containerBuilder);
    }
}
