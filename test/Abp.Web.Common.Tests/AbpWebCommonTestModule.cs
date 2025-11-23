using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.TestBase;

namespace Abp.Web.Common.Tests
{
    [DependsOn(typeof(AbpWebCommonModule), typeof(AbpTestBaseModule))]
    public class AbpWebCommonTestModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.RegisterAssemblyByConvention(typeof(AbpWebCommonTestModule).GetAssembly());
            Configuration.Settings.Providers.Add<AbpWebCommonTestModuleSettingProvider>();
            Configuration.Authorization.Providers.Add<AbpWebCommonTestModuleAuthProvider>();
        }


        public override void Initialize()
        {
        }
    }
}
