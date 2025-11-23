using System;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Modules;
using Abp.Notifications;
using Abp.Reflection.Extensions;
using Abp.TestBase;
using Abp.Zero.Configuration;
using Abp.Zero.Notifications;
using Abp.ZeroCore.SampleApp;
using Abp.Configuration.Startup;
using Autofac;

namespace Abp.Zero;

[DependsOn(typeof(AbpZeroCoreSampleAppModule), typeof(AbpTestBaseModule))]
public class AbpZeroTestModule : AbpModule {
    public AbpZeroTestModule(AbpZeroCoreSampleAppModule sampleAppModule) {
        sampleAppModule.SkipDbContextRegistration = true;
    }

    public override void ConfigureServices() {
        TestServiceCollectionRegistrar.Register(IocManager);
        IocManager.RegisterAssemblyByConvention(typeof(AbpZeroTestModule).GetAssembly());

        Configuration.ReplaceService<INotificationDistributer, FakeNotificationDistributer>();
        // Autofac has built-in support for Lazy<T>, no need for special component loader

#pragma warning disable CS0618 // Type or member is obsolete, this line will be removed once the UseStaticMapper is removed
        Configuration.Modules.AbpAutoMapper().UseStaticMapper = false;
#pragma warning restore CS0618 // Type or member is obsolete, this line will be removed once the UseStaticMapper is removed
        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();
        Configuration.UnitOfWork.IsTransactional = false;
    }

    public override void Initialize() {
    }
}