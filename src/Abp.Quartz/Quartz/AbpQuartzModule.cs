using System.Reflection;
using Abp.Dependency;
using Abp.Modules;
using Abp.Quartz.Configuration;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Quartz;

namespace Abp.Quartz
{
    [DependsOn(typeof (AbpKernelModule))]
    public class AbpQuartzModule : AbpModule
    {
        private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

        public override void ConfigureServices() {
            IocManager.Register<IAbpQuartzConfiguration, AbpQuartzConfiguration>();

            IocManager.RegisterIfNot<IJobListener, AbpQuartzJobListener>();

            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
            OneTimeRunner.Run(() =>
            {
                Configuration.Modules.AbpQuartz().Scheduler.JobFactory = new AbpQuartzJobFactory(IocManager);
            });


            Configuration.Modules.AbpQuartz().Scheduler.ListenerManager.AddJobListener(IocManager.Resolve<IJobListener>());

        }

        public override void Initialize()
        {
            if (Configuration.BackgroundJobs.IsJobExecutionEnabled)
            {
                IocManager.Resolve<IBackgroundWorkerManager>().Add(IocManager.Resolve<IQuartzScheduleJobManager>());
            }

        }
    }
}
