using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero;

namespace Abp.OpenIddict;

[DependsOn(typeof(AbpZeroCoreModule))]
public class AbpZeroCoreOpenIddictModule : AbpModule
{
    public override void ConfigureServices() {
        IocManager.RegisterAssemblyByConvention(typeof(AbpZeroCoreOpenIddictModule).GetAssembly());

    }

    public override void Initialize()
    {
    }
}