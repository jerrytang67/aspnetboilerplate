using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Dependency;
using Abp.AspNetCore.EmbeddedResources;
using Abp.AspNetCore.Mvc;
using Abp.Dependency;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Abp.AspNetCore.Mvc.Providers;
using Abp.AspNetCore.Webhook;
using Abp.Auditing;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Json.SystemTextJson;
using Abp.Modules;
using Abp.PlugIns;
using Abp.Runtime.Validation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

namespace Abp.AspNetCore;

public static class AbpServiceCollectionExtensions
{
    /// <summary>
    /// Integrates ABP to AspNet Core.
    /// </summary>
    /// <typeparam name="TStartupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</typeparam>
    /// <param name="services">Services.</param>
    /// <param name="optionsAction">An action to get/modify options</param>
    /// <param name="removeConventionalInterceptors">Removes the conventional interceptors</param>
    public static IServiceProvider AddAbp<TStartupModule>(this IServiceCollection services,
        [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null,
        bool removeConventionalInterceptors = true)
        where TStartupModule : AbpModule
    {
        if (removeConventionalInterceptors)
        {
            RemoveConventionalInterceptionSelectors();
        }

        // Create bootstrapper and get IocManager
        var abpBootstrapper = AbpBootstrapper.Create<TStartupModule>(optionsAction);
        var iocManager = (IocManager)abpBootstrapper.IocManager;

        // PHASE 1: Populate Autofac builder with ASP.NET Core services FIRST
        // This MUST be done before any Autofac modules are registered to avoid pipeline building issues
        iocManager.Builder.Populate(services);

        // PHASE 2: Register core infrastructure services BEFORE loading modules
        // These services (TypeFinder, LocalizationManager, etc.) must be available during ConfigureServices()
        Abp.Dependency.Installers.AbpCoreModule.RegisterCoreServices(iocManager.Builder);
        RegisterWebCommonConfigurationsIfAvailable(iocManager);
        iocManager.Builder.RegisterInstance(abpBootstrapper).As<AbpBootstrapper>().SingleInstance();

        // Configure ASP.NET Core services
        ConfigureAspNetCore(services, abpBootstrapper.IocManager);

        // PHASE 3: Load all modules and create AbpModuleManager
        var moduleManager = CreateAndInitializeModuleManager(abpBootstrapper, iocManager);

        // PHASE 4: Call ConfigureServices on all modules before container build
        moduleManager.ConfigureServices();

        // PHASE 5: Build Autofac container after all registrations
        iocManager.BuildContainer();

        // PHASE 6: Return AutofacServiceProvider wrapping the built container
        return new AutofacServiceProvider(iocManager.IocContainer);
    }

    private static AbpModuleManager CreateAndInitializeModuleManager(AbpBootstrapper abpBootstrapper, IocManager iocManager)
    {
        // Resolve plugin manager and module manager from the container
        // (they were registered by AbpCoreModule)
        var plugInManager = new AbpPlugInManager();
        var moduleManager = new AbpModuleManager(iocManager);
        
        // Initialize module manager with startup module (this loads all modules)
        moduleManager.Initialize(abpBootstrapper.StartupModule);

        // Create Configuration instance manually and set it on all modules
        // This is needed because ConfigureServices needs access to Configuration
        // but the container isn't built yet
        var configuration = new Abp.Configuration.Startup.AbpStartupConfiguration(iocManager);
        configuration.EarlyInitialize(); // Initialize configuration objects before ConfigureServices
        
        // Create ASP.NET Core configuration and store it in the dictionary
        // so Get<IAbpAspNetCoreConfiguration>() can find it without resolving from container
        var aspNetCoreConfig = new Configuration.AbpAspNetCoreConfiguration();
        configuration[typeof(Configuration.IAbpAspNetCoreConfiguration).FullName] = aspNetCoreConfig;
        
        foreach (var module in moduleManager.Modules)
        {
            module.Instance.Configuration = configuration;
        }

        // Register the initialized instances, replacing the ones from AbpCoreModule
        // IMPORTANT: Use ExternallyOwned() to prevent property injection which can cause circular dependencies
        iocManager.Builder.RegisterInstance(configuration)
            .As<AbpStartupConfiguration>()
            .As<IAbpStartupConfiguration>()
            .SingleInstance()
            .ExternallyOwned();  // Prevents Autofac from injecting properties or disposing

        // Register all configuration sub-objects that were created in EarlyInitialize()
        // This ensures that any filters or settings configured during ConfigureServices are preserved
        iocManager.Builder.RegisterInstance(configuration.UnitOfWork)
            .As<UnitOfWorkDefaultOptions>()
            .As<IUnitOfWorkDefaultOptions>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Localization)
            .As<ILocalizationConfiguration>()
            .As<LocalizationConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Navigation)
            .As<INavigationConfiguration>()
            .As<NavigationConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Authorization)
            .As<IAuthorizationConfiguration>()
            .As<AuthorizationConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Validation)
            .As<IValidationConfiguration>()
            .As<ValidationConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Settings)
            .As<ISettingsConfiguration>()
            .As<SettingsConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Modules)
            .As<Abp.Configuration.Startup.IModuleConfigurations>()
            .As<Abp.Configuration.Startup.ModuleConfigurations>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Features)
            .As<Abp.Application.Features.IFeatureConfiguration>()
            .As<Abp.Application.Features.FeatureConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.BackgroundJobs)
            .As<Abp.BackgroundJobs.IBackgroundJobConfiguration>()
            .As<Abp.BackgroundJobs.BackgroundJobConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Notifications)
            .As<Abp.Notifications.INotificationConfiguration>()
            .As<Abp.Notifications.NotificationConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.EventBus)
            .As<IEventBusConfiguration>()
            .As<EventBusConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Auditing)
            .As<Abp.Auditing.IAuditingConfiguration>()
            .As<Abp.Auditing.AuditingConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Caching)
            .As<Abp.Runtime.Caching.Configuration.ICachingConfiguration>()
            .As<Abp.Runtime.Caching.Configuration.CachingConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.MultiTenancy)
            .As<IMultiTenancyConfig>()
            .As<MultiTenancyConfig>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.EmbeddedResources)
            .As<Abp.Resources.Embedded.IEmbeddedResourcesConfiguration>()
            .As<Abp.Resources.Embedded.EmbeddedResourcesConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.EntityHistory)
            .As<Abp.EntityHistory.IEntityHistoryConfiguration>()
            .As<Abp.EntityHistory.EntityHistoryConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.Webhooks)
            .As<Abp.Webhooks.IWebhooksConfiguration>()
            .As<Abp.Webhooks.WebhooksConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(configuration.DynamicEntityProperties)
            .As<Abp.DynamicEntityProperties.IDynamicEntityPropertyConfiguration>()
            .As<Abp.DynamicEntityProperties.DynamicEntityPropertyConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        iocManager.Builder.RegisterInstance(plugInManager)
            .As<AbpPlugInManager>()
            .As<IAbpPlugInManager>()
            .SingleInstance();

        iocManager.Builder.RegisterInstance(moduleManager)
            .As<AbpModuleManager>()
            .As<IAbpModuleManager>()
            .SingleInstance();

        return moduleManager;
    }

    /// <summary>
    /// Integrates ABP to AspNet Core using the modern HostBuilder pattern.
    /// This method prepares ABP for container building but does NOT build the container.
    /// The container will be built by the HostBuilder.
    /// </summary>
    /// <typeparam name="TStartupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</typeparam>
    /// <param name="services">Services.</param>
    /// <param name="optionsAction">An action to get/modify options</param>
    /// <param name="removeConventionalInterceptors">Removes the conventional interceptors</param>
    /// <returns>A service provider factory that will build the Autofac container</returns>
    public static IServiceProviderFactory<ContainerBuilder> AddAbpWithoutBuildingContainer<TStartupModule>(
        this IServiceCollection services,
        [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null,
        bool removeConventionalInterceptors = true)
        where TStartupModule : AbpModule
    {
        if (removeConventionalInterceptors)
        {
            RemoveConventionalInterceptionSelectors();
        }

        // Create bootstrapper and get IocManager
        var abpBootstrapper = AbpBootstrapper.Create<TStartupModule>(optionsAction);
        var iocManager = (IocManager)abpBootstrapper.IocManager;

        // Register AbpBootstrapper in services collection so it can be found by conventions
        services.AddSingleton(abpBootstrapper);

        // PHASE 1: Configure ASP.NET Core services FIRST (registers IHttpContextAccessor, etc.)
        ConfigureAspNetCore(services, abpBootstrapper.IocManager);

        // PHASE 2: Populate Autofac builder with ASP.NET Core services (including IHttpContextAccessor)
        iocManager.Builder.Populate(services);

        // PHASE 3: Register core infrastructure services BEFORE loading modules
        // These services (TypeFinder, LocalizationManager, etc.) must be available during ConfigureServices()
        Abp.Dependency.Installers.AbpCoreModule.RegisterCoreServices(iocManager.Builder);
        RegisterWebCommonConfigurationsIfAvailable(iocManager);
        iocManager.Builder.RegisterInstance(abpBootstrapper).As<AbpBootstrapper>().SingleInstance();

        // Register IHttpContextAccessor directly in Autofac as a workaround
        // This ensures it's available even if the Populate didn't include it
        iocManager.Builder.RegisterType<HttpContextAccessor>()
            .As<IHttpContextAccessor>()
            .SingleInstance()
            .ExternallyOwned();  // Prevents property injection

        // PHASE 4: Load all modules and create AbpModuleManager
        var moduleManager = CreateAndInitializeModuleManager(abpBootstrapper, iocManager);

        // PHASE 5: Call ConfigureServices on all modules before container build
        moduleManager.ConfigureServices();

        // PHASE 6: Return a factory that will build the container when HostBuilder calls it
        return new AbpAutofacServiceProviderFactory(iocManager);
    }

    /// <summary>
    /// Integrates ABP to AspNet Core without creating a IServiceProvider.
    /// </summary>
    /// <typeparam name="TStartupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</typeparam>
    /// <param name="services">Services.</param>
    /// <param name="optionsAction">An action to get/modify options</param>
    /// <param name="removeConventionalInterceptors">Removes the conventional interceptors</param>
    public static void AddAbpWithoutCreatingServiceProvider<TStartupModule>(this IServiceCollection services,
        [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null,
        bool removeConventionalInterceptors = true)
        where TStartupModule : AbpModule
    {
        if (removeConventionalInterceptors)
        {
            RemoveConventionalInterceptionSelectors();
        }

        // Create bootstrapper and get IocManager
        var abpBootstrapper = AbpBootstrapper.Create<TStartupModule>(optionsAction);
        var iocManager = (IocManager)abpBootstrapper.IocManager;

        // Register core infrastructure services
        // These services (TypeFinder, LocalizationManager, etc.) must be available during ConfigureServices()
        Abp.Dependency.Installers.AbpCoreModule.RegisterCoreServices(iocManager.Builder);
        RegisterWebCommonConfigurationsIfAvailable(iocManager);
        iocManager.Builder.RegisterInstance(abpBootstrapper).As<AbpBootstrapper>().SingleInstance();

        ConfigureAspNetCore(services, abpBootstrapper.IocManager);
    }

    private static void RemoveConventionalInterceptionSelectors()
    {
        UnitOfWorkDefaultOptions.ConventionalUowSelectorList = new List<Func<Type, bool>>();
        AbpAuditingDefaultOptions.ConventionalAuditingSelectorList = new List<Func<Type, bool>>();
        AbpValidationDefaultOptions.ConventionalValidationSelectorList = new List<Func<Type, bool>>();
    }

    private static void ConfigureAspNetCore(IServiceCollection services, IIocResolver iocResolver)
    {
        //See https://github.com/aspnet/Mvc/issues/3936 to know why we added these services.
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

        //Use DI to create controllers
        services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

        //Use DI to create page models
        services.Replace(ServiceDescriptor
            .Singleton<IPageModelActivatorProvider, ServiceBasedPageModelActivatorProvider>());

        //Use DI to create view components
        services.Replace(ServiceDescriptor
            .Singleton<IViewComponentActivator, ServiceBasedViewComponentActivator>());

        //Add feature providers
        var partManager = services.GetSingletonServiceOrNull<ApplicationPartManager>();
        Console.WriteLine($"[DEBUG] ApplicationPartManager is {(partManager == null ? "NULL" : "NOT NULL")}");
        if (partManager != null)
        {
            Console.WriteLine($"[DEBUG] Adding AbpAppServiceControllerFeatureProvider");
            partManager.FeatureProviders.Add(new AbpAppServiceControllerFeatureProvider(iocResolver));
            Console.WriteLine($"[DEBUG] Total FeatureProviders: {partManager.FeatureProviders.Count}");
        }
        else
        {
            Console.WriteLine("[DEBUG] WARNING: ApplicationPartManager is null! Dynamic API controllers will not be registered.");
        }

        //Configure System Text JSON serializer
        services.AddOptions<JsonOptions>()
            .Configure<IServiceProvider>((options, rootServiceProvider) =>
        {
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            options.JsonSerializerOptions.AllowTrailingCommas = true;

            options.JsonSerializerOptions.Converters.Add(new AbpStringToEnumFactory());
            options.JsonSerializerOptions.Converters.Add(new AbpStringToBooleanConverter());
            options.JsonSerializerOptions.Converters.Add(new AbpStringToGuidConverter());
            options.JsonSerializerOptions.Converters.Add(new AbpNullableStringToGuidConverter());
            options.JsonSerializerOptions.Converters.Add(new AbpNullableFromEmptyStringConverterFactory());
            options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());

            var aspNetCoreConfiguration = rootServiceProvider.GetRequiredService<IAbpAspNetCoreConfiguration>();
            options.JsonSerializerOptions.TypeInfoResolver = new AbpDateTimeJsonTypeInfoResolver(aspNetCoreConfiguration.InputDateTimeFormats, aspNetCoreConfiguration.OutputDateTimeFormat);
        });

        //Configure MVC
        services.Configure<MvcOptions>(mvcOptions => { mvcOptions.AddAbp(services); });

        //Configure Razor
        services.Insert(0,
            ServiceDescriptor.Singleton<IConfigureOptions<MvcRazorRuntimeCompilationOptions>>(
                new ConfigureOptions<MvcRazorRuntimeCompilationOptions>(
                    (options) => { options.FileProviders.Add(new EmbeddedResourceViewFileProvider(iocResolver)); }
                )
            )
        );

        services.AddHttpClient(AspNetCoreWebhookSender.WebhookSenderHttpClientName);
    }



    private static void RegisterWebCommonConfigurationsIfAvailable(IocManager iocManager)
    {
        // Register all module configuration services that are accessed in PreInitialize()
        // This is necessary because Autofac requires all registrations before container build

        // Web Common configurations
        var webCommonTypes = new[]
        {
            ("Abp.Web.MultiTenancy.IWebMultiTenancyConfiguration, Abp.Web.Common", "Abp.Web.MultiTenancy.WebMultiTenancyConfiguration, Abp.Web.Common"),
            ("Abp.Web.Api.ProxyScripting.Configuration.IApiProxyScriptingConfiguration, Abp.Web.Common", "Abp.Web.Api.ProxyScripting.Configuration.ApiProxyScriptingConfiguration, Abp.Web.Common"),
            ("Abp.Web.Security.AntiForgery.IAbpAntiForgeryConfiguration, Abp.Web.Common", "Abp.Web.Security.AntiForgery.AbpAntiForgeryConfiguration, Abp.Web.Common"),
            ("Abp.Web.Configuration.IWebEmbeddedResourcesConfiguration, Abp.Web.Common", "Abp.Web.Configuration.WebEmbeddedResourcesConfiguration, Abp.Web.Common"),
            ("Abp.Web.Configuration.IAbpWebCommonModuleConfiguration, Abp.Web.Common", "Abp.Web.Configuration.AbpWebCommonModuleConfiguration, Abp.Web.Common")
        };

        // AutoMapper configurations
        var autoMapperTypes = new[]
        {
            ("Abp.AutoMapper.IAbpAutoMapperConfiguration, Abp.AutoMapper", "Abp.AutoMapper.AbpAutoMapperConfiguration, Abp.AutoMapper")
        };

        // EntityFramework Core configurations
        var efCoreTypes = new[]
        {
            ("Abp.EntityFrameworkCore.Configuration.IAbpEfCoreConfiguration, Abp.EntityFrameworkCore", "Abp.EntityFrameworkCore.Configuration.AbpEfCoreConfiguration, Abp.EntityFrameworkCore")
        };

        // HTML Sanitizer configurations
        var htmlSanitizerTypes = new[]
        {
            ("Abp.HtmlSanitizer.Configuration.IAbpHtmlSanitizerModuleConfiguration, Abp.HtmlSanitizer", "Abp.HtmlSanitizer.Configuration.AbpHtmlSanitizerModuleConfiguration, Abp.HtmlSanitizer"),
            ("Abp.HtmlSanitizer.Configuration.IHtmlSanitizerConfiguration, Abp.HtmlSanitizer", "Abp.HtmlSanitizer.Configuration.HtmlSanitizerConfiguration, Abp.HtmlSanitizer")
        };

        // Register all module configurations
        var allTypes = webCommonTypes.Concat(autoMapperTypes).Concat(efCoreTypes).Concat(htmlSanitizerTypes);

        foreach (var (interfaceTypeName, implTypeName) in allTypes)
        {
            try
            {
                var interfaceType = Type.GetType(interfaceTypeName);
                var implType = Type.GetType(implTypeName);

                if (interfaceType != null && implType != null)
                {
                    iocManager.Register(interfaceType, implType);
                }
            }
            catch
            {
                // Ignore if types cannot be loaded (assembly not referenced)
            }
        }

        // Register ASP.NET Core configurations directly (no need for Type.GetType since we're in the same assembly)
        iocManager.Register<Configuration.IAbpAspNetCoreConfiguration, Configuration.AbpAspNetCoreConfiguration>();
    }
}