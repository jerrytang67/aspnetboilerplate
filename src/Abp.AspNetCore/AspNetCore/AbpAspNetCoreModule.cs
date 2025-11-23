using System;
using System.Linq;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.MultiTenancy;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Auditing;
using Abp.AspNetCore.Mvc.Caching;
using Abp.AspNetCore.Mvc.ExceptionHandling;
using Abp.AspNetCore.Mvc.Results;
using Abp.AspNetCore.Mvc.Uow;
using Abp.AspNetCore.Mvc.Validation;
using Abp.AspNetCore.PlugIn;
using Abp.AspNetCore.Runtime.Session;
using Abp.AspNetCore.Security.AntiForgery;
using Abp.AspNetCore.Webhook;
using Abp.Auditing;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Session;
using Abp.Web;
using Abp.Web.Security.AntiForgery;
using Abp.Webhooks;
using Autofac;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Abp.AspNetCore;

[DependsOn(typeof(AbpWebCommonModule))]
public class AbpAspNetCoreModule : AbpModule {
    public override void ConfigureServices() {
        IocManager.Register<IAsyncAuthorizationFilter, Abp.AspNetCore.Mvc.Authorization.AbpAuthorizationFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncActionFilter, AbpAuditActionFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncActionFilter, AbpValidationActionFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncActionFilter, AbpUowActionFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IExceptionFilter, AbpExceptionFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IResultFilter, AbpResultFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncPageFilter, AbpUowPageFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncPageFilter, AbpAuditPageFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncPageFilter, AbpResultPageFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncPageFilter, AbpExceptionPageFilter>(DependencyLifeStyle.Transient);
        IocManager.Register<IAsyncAuthorizationFilter, AbpAutoValidateAntiforgeryTokenAuthorizationFilter>(DependencyLifeStyle.Transient);

        // Register conventional registrar
        IocManager.AddConventionalRegistrar(new AbpAspNetCoreConventionalRegistrar());

        // IMPORTANT: Get or create the configuration instance from the Configuration dictionary.
        // This ensures that if it was already created in AbpServiceCollectionExtensions, we use the same instance.
        // Otherwise, create a new one and store it.
        var aspNetCoreConfig = Configuration.Get(typeof(IAbpAspNetCoreConfiguration).FullName) as AbpAspNetCoreConfiguration;
        if (aspNetCoreConfig == null) {
            aspNetCoreConfig = new AbpAspNetCoreConfiguration();
            Configuration.Set(typeof(IAbpAspNetCoreConfiguration).FullName, aspNetCoreConfig);
        }

        // Register the same instance in the IoC container as a singleton
        // This ensures that Configuration.Modules.AbpAspNetCore() and IocManager.Resolve<IAbpAspNetCoreConfiguration>()
        // return the SAME instance, which is critical for dynamic API controller registration.
        var iocManager = (IocManager)IocManager;
        iocManager.Builder.RegisterInstance(aspNetCoreConfig)
            .As<IAbpAspNetCoreConfiguration>()
            .As<AbpAspNetCoreConfiguration>()
            .SingleInstance()
            .ExternallyOwned();

        // Replace services with ASP.NET Core implementations
        Configuration.ReplaceService<IPrincipalAccessor, AspNetCorePrincipalAccessor>(DependencyLifeStyle.Transient);
        Configuration.ReplaceService<IAbpAntiForgeryManager, AbpAspNetCoreAntiForgeryManager>(DependencyLifeStyle.Transient);
        Configuration.ReplaceService<IClientInfoProvider, HttpContextClientInfoProvider>(DependencyLifeStyle.Transient);
        Configuration.ReplaceService<IWebhookSender, AspNetCoreWebhookSender>(DependencyLifeStyle.Transient);

        // Register per-user configuration
        IocManager.Register<IGetScriptsResponsePerUserConfiguration, GetScriptsResponsePerUserConfiguration>();

        // Register assembly by convention
        IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetCoreModule).GetAssembly());
    }


    public override void Initialize() {
        AddApplicationParts();

        // Configuration that uses registered services (moved from ConfigureServices)
        // These must be done AFTER container is built because they resolve services from the container
        ConfigureAntiforgery();

        Configuration.Modules.AbpAspNetCore().FormBodyBindingIgnoredTypes.Add(typeof(IFormFile));

        Configuration.MultiTenancy.Resolvers.Add<DomainTenantResolveContributor>();
        Configuration.MultiTenancy.Resolvers.Add<HttpHeaderTenantResolveContributor>();
        Configuration.MultiTenancy.Resolvers.Add<HttpCookieTenantResolveContributor>();

        Configuration.Caching.Configure(GetScriptsResponsePerUserCache.CacheName, cache => { cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(30); });
    }


    private void AddApplicationParts() {
        var configuration = IocManager.Resolve<AbpAspNetCoreConfiguration>();
        var partManager = IocManager.Resolve<ApplicationPartManager>();
        var moduleManager = IocManager.Resolve<IAbpModuleManager>();

        partManager.AddApplicationPartsIfNotAddedBefore(typeof(AbpAspNetCoreModule).Assembly);

        var controllerAssemblies = configuration.ControllerAssemblySettings.Select(s => s.Assembly).Distinct();
        foreach (var controllerAssembly in controllerAssemblies) {
            partManager.AddApplicationPartsIfNotAddedBefore(controllerAssembly);
        }

        var plugInAssemblies = moduleManager.Modules.Where(m => m.IsLoadedAsPlugIn).Select(m => m.Assembly).Distinct();
        foreach (var plugInAssembly in plugInAssemblies) {
            partManager.AddAbpPlugInAssemblyPartIfNotAddedBefore(new AbpPlugInAssemblyPart(plugInAssembly));
        }
    }

    private void ConfigureAntiforgery() {
        IocManager.Using<IOptions<AntiforgeryOptions>>(optionsAccessor => { optionsAccessor.Value.HeaderName = Configuration.Modules.AbpWebCommon().AntiForgery.TokenHeaderName; });
    }
}