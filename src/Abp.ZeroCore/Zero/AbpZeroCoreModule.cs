using System;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Localization.Dictionaries.Xml;
using Abp.Localization.Sources;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Threading.BackgroundWorkers;
using Abp.Zero.Configuration;
using Autofac;

namespace Abp.Zero;

[DependsOn(typeof(AbpZeroCommonModule))]
public class AbpZeroCoreModule : AbpModule {
    public override void ConfigureServices() {
        // Register assembly by convention - must happen before container build
        IocManager.RegisterAssemblyByConvention(typeof(AbpZeroCoreModule).GetAssembly());

        // Register the open generic UserTokenExpirationWorker type
        // The concrete type will be resolved at runtime based on IAbpZeroEntityTypes
        var iocMgr = (IocManager)IocManager;
        iocMgr.Builder.RegisterGeneric(typeof(UserTokenExpirationWorker<,>)).AsSelf().InstancePerDependency();

        // Localization configuration - uses Configuration which is available
        Configuration.Localization.Sources.Extensions.Add(
            new LocalizationSourceExtensionInfo(
                AbpZeroConsts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(AbpZeroCoreModule).GetAssembly(), "Abp.Zero.Localization.SourceExt"
                )
            )
        );

    }

    public override void Initialize() {

        // No service registrations - all moved to ConfigureServices
        // Start background worker - requires resolved services
        if (Configuration.BackgroundJobs.IsJobExecutionEnabled) {
            using var entityTypes = IocManager.ResolveAsDisposable<IAbpZeroEntityTypes>();
            var implType = typeof(UserTokenExpirationWorker<,>)
                .MakeGenericType(entityTypes.Object.Tenant, entityTypes.Object.User);
            var worker = IocManager.Resolve(implType) as IBackgroundWorker;
            var workerManager = IocManager.Resolve<IBackgroundWorkerManager>();
            workerManager.Add(worker);
        }


    }
}