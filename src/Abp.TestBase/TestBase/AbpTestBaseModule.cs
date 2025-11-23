using Abp.Modules;
using Abp.Reflection.Extensions;

namespace Abp.TestBase
{
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpTestBaseModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.RegisterAssemblyByConvention(typeof(AbpTestBaseModule).GetAssembly());
            Configuration.EventBus.UseDefaultEventBus = false;
            Configuration.DefaultNameOrConnectionString = "Default";
        }


        public override void Initialize()
        {

        }
    }
}