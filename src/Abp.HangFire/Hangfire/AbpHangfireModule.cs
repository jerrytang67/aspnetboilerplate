using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Hangfire;

namespace Abp.Hangfire
{
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpHangfireModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.Register<IAbpHangfireConfiguration, AbpHangfireConfiguration>();

            Configuration.Modules
                .AbpHangfire()
                .GlobalConfiguration
                .UseActivator(new HangfireIocJobActivator(IocManager));
            IocManager.RegisterAssemblyByConvention(typeof(AbpHangfireModule).GetAssembly());

            GlobalJobFilters.Filters.Add(IocManager.Resolve<AbpHangfireJobExceptionFilter>());

        }

        public override void Initialize()
        {
        }
    }
}
