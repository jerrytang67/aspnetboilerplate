using System.Reflection;
using Abp.Modules;
using Abp.OpenIddict;

namespace Abp.AspNetCore.OpenIddict;

[DependsOn(typeof(AbpAspNetCoreModule), typeof(AbpZeroCoreOpenIddictModule))]
public class AbpAspNetCoreOpenIddictModule : AbpModule
{
    public override void ConfigureServices() {
        IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

    }

    public override void Initialize()
    {
    }
}