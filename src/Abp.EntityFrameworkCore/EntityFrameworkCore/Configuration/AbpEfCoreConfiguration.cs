using System;
using Abp.Dependency;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Abp.EntityFrameworkCore.Configuration;

public class AbpEfCoreConfiguration : IAbpEfCoreConfiguration
{
    private readonly IIocManager _iocManager;

    public AbpEfCoreConfiguration(IIocManager iocManager)
    {
        _iocManager = iocManager;
    }

    public bool UseAbpQueryCompiler { get; set; } = false;

    public void AddDbContext<TDbContext>(Action<AbpDbContextConfiguration<TDbContext>> action)
        where TDbContext : DbContext
    {
        var iocMgr = (IocManager)_iocManager;
        iocMgr.Builder.RegisterInstance(new AbpDbContextConfigurerAction<TDbContext>(action))
            .As<IAbpDbContextConfigurer<TDbContext>>();
    }
}
