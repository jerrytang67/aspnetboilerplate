using Abp.Application.Features;
using Abp.Auditing;
using Abp.BackgroundJobs;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Localization;
using Abp.Modules;
using Abp.Notifications;
using Abp.Reflection;
using Abp.Resources.Embedded;
using Abp.Runtime.Caching.Configuration;
using Abp.Runtime.Validation;
using Abp.Webhooks;
using Autofac;

namespace Abp.Dependency.Installers
{
    /// <summary>
    /// Helper class that registers core ABP services directly to ContainerBuilder.
    /// This is NOT an Autofac Module because Module.Load() only executes during Build(),
    /// which is too late - core services must be registered BEFORE ConfigureServices() phase.
    /// </summary>
    internal static class AbpCoreModule
    {
        /// <summary>
        /// Registers core ABP infrastructure services that must be available during Initialize().
        /// This method should be called EARLY in the initialization process, before loading ABP modules.
        ///
        /// Note: Configuration objects are NOT registered here - they are created by EarlyInitialize()
        /// and registered as instances by CreateAndInitializeModuleManager().
        /// </summary>
        public static void RegisterCoreServices(ContainerBuilder builder)
        {
            // Register core infrastructure services that are needed during Initialize() phase
            // These services have dependencies but are only resolved AFTER ConfigureServices completes

            // TypeFinder and AssemblyFinder - needed for reflection operations
            builder.RegisterType<TypeFinder>().As<ITypeFinder>().As<TypeFinder>().SingleInstance().ExternallyOwned();
            builder.RegisterType<AbpAssemblyFinder>().As<IAssemblyFinder>().As<AbpAssemblyFinder>().SingleInstance().ExternallyOwned();

            // LocalizationManager - needed in Initialize() phase
            builder.RegisterType<LocalizationManager>().As<ILocalizationManager>().As<LocalizationManager>().SingleInstance().ExternallyOwned();

            // DynamicEntityPropertyDefinitionContext - transient service
            builder.RegisterType<DynamicEntityPropertyDefinitionContext>().As<IDynamicEntityPropertyDefinitionContext>().As<DynamicEntityPropertyDefinitionContext>().InstancePerDependency();

            // Note: The following are NOT registered here because they are created by EarlyInitialize()
            // and registered as instances in CreateAndInitializeModuleManager():
            // - AbpStartupConfiguration (and all its sub-configurations)
            // - AbpModuleManager (created manually in CreateAndInitializeModuleManager)
        }
    }
}
