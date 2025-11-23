using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AbpAspNetCoreDemo.IntegrationTests;

/// <summary>
/// Custom web application factory for integration tests with Autofac support
/// Allows overriding services in the Autofac container for testing purposes
/// </summary>
public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly Action<ContainerBuilder> _containerConfiguration;

    /// <summary>
    /// Creates a new instance of the factory
    /// </summary>
    /// <param name="containerConfiguration">Optional action to configure the Autofac container for test service overrides</param>
    public CustomWebApplicationFactory(Action<ContainerBuilder> containerConfiguration = null)
    {
        _containerConfiguration = containerConfiguration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_containerConfiguration != null)
        {
            builder.ConfigureServices(services =>
            {
                // The services here are already configured by ABP
                // We'll override them in ConfigureContainer
            });
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (_containerConfiguration != null)
        {
            // Configure the Autofac container to override services
            builder.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
            {
                _containerConfiguration(containerBuilder);
            });
        }

        return base.CreateHost(builder);
    }
}