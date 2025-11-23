using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.Logging;
using Quartz;

namespace Abp.Quartz {
    public class AbpQuartzJobListener(ILogger<AbpQuartzJobListener> logger) : IJobListener {
        public string Name { get; } = "AbpJobListener";

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken)) {
            logger.LogDebug($"Job {context.JobDetail.JobType.Name} executing...");
            return Task.FromResult(0);
        }

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken)) {
            logger.LogInformation($"Job {context.JobDetail.JobType.Name} executing operation vetoed...");
            return Task.FromResult(0);
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken)) {
            if (jobException == null) {
                logger.LogDebug($"Job {context.JobDetail.JobType.Name} successfully executed.");
            }
            else {
                logger.LogError($"Job {context.JobDetail.JobType.Name} failed with exception: {jobException}");
            }

            return Task.FromResult(0);
        }
    }
}