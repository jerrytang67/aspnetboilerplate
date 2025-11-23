using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.Web.Api.ProxyScripting.Configuration;
using Abp.Web.Api.ProxyScripting.Generators.JQuery;
using Abp.Web.Configuration;
using Abp.Web.MultiTenancy;
using Abp.Web.Security.AntiForgery;
using Abp.Reflection.Extensions;
using Abp.Web.Minifier;

namespace Abp.Web
{
    /// <summary>
    /// This module is used to use ABP in ASP.NET web applications.
    /// </summary>
    [DependsOn(typeof(AbpKernelModule))]    
    public class AbpWebCommonModule : AbpModule
    {
        /// <inheritdoc/>
        public override void ConfigureServices()
        {
            // Register configuration services
            IocManager.Register<IWebMultiTenancyConfiguration, WebMultiTenancyConfiguration>();
            IocManager.Register<IApiProxyScriptingConfiguration, ApiProxyScriptingConfiguration>();
            IocManager.Register<IAbpAntiForgeryConfiguration, AbpAntiForgeryConfiguration>();
            IocManager.Register<IWebEmbeddedResourcesConfiguration, WebEmbeddedResourcesConfiguration>();
            IocManager.Register<IAbpWebCommonModuleConfiguration, AbpWebCommonModuleConfiguration>();
            IocManager.Register<IJavaScriptMinifier, NUglifyJavaScriptMinifier>();

            // IMPORTANT: Store the configuration instance in the Configuration dictionary
            // Create configuration with its dependencies
            var multiTenancyConfig = new WebMultiTenancyConfiguration();
            var apiProxyScriptingConfig = new ApiProxyScriptingConfiguration();
            var antiForgeryConfig = new AbpAntiForgeryConfiguration();
            var embeddedResourcesConfig = new WebEmbeddedResourcesConfiguration();
            var webCommonConfig = new AbpWebCommonModuleConfiguration(
                apiProxyScriptingConfig,
                antiForgeryConfig,
                embeddedResourcesConfig,
                multiTenancyConfig
            );

            Configuration.Set(typeof(IAbpWebCommonModuleConfiguration).FullName, webCommonConfig);

            // Register assembly by convention
            IocManager.RegisterAssemblyByConvention(typeof(AbpWebCommonModule).GetAssembly());


            // Localization source registration
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    AbpWebConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AbpWebCommonModule).GetAssembly(), "Abp.Web.Localization.AbpWebXmlSource"
                    )));
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            // Configuration logic that uses registered services (moved from ConfigureServices)
            // This must be done AFTER container is built because it resolves services from the container
            Configuration.Modules.AbpWebCommon().ApiProxyScripting.Generators[JQueryProxyScriptGenerator.Name] = typeof(JQueryProxyScriptGenerator);
        }
    }
}
