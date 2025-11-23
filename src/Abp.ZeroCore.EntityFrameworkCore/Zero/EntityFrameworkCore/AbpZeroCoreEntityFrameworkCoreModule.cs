using System;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Modules;
using Abp.MultiTenancy;
using Abp.Reflection.Extensions;
using Autofac;

namespace Abp.Zero.EntityFrameworkCore;

/// <summary>
/// Entity framework integration module for ASP.NET Boilerplate Zero.
/// </summary>
[DependsOn(typeof(AbpZeroCoreModule), typeof(AbpEntityFrameworkCoreModule))]
public class AbpZeroCoreEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register Zero-specific repository implementations and DbContext configurations
        // All service registrations must happen before container build
        IocManager.RegisterAssemblyByConvention(typeof(AbpZeroCoreEntityFrameworkCoreModule).GetAssembly());
        
        // Register DbPerTenantConnectionStringResolver
        Configuration.ReplaceService(typeof(IConnectionStringResolver), () =>
        {
            var iocMgr = (IocManager)IocManager;
            iocMgr.Builder.RegisterType<DbPerTenantConnectionStringResolver>()
                .As<IConnectionStringResolver>()
                .As<IDbPerTenantConnectionStringResolver>()
                .InstancePerDependency();
        });
    }

    public override void Initialize()
    {
        // No service registrations - all moved to ConfigureServices
    }
}
