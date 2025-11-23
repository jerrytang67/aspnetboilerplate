using Abp.Modules;
using Abp.Reflection.Extensions;

namespace Abp.EntityFramework {
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpEntityFrameworkCommonModule : AbpModule {
        public override void ConfigureServices() {
            IocManager.RegisterAssemblyByConvention(typeof(AbpEntityFrameworkCommonModule).GetAssembly());
        }

        public override void Initialize() {
        }
    }
}