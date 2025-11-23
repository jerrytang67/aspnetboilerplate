using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.Logging;

namespace Abp.Auditing
{
    /// <summary>
    /// Implements <see cref="IAuditingStore"/> to simply write audits to logs.
    /// </summary>
    public class SimpleLogAuditingStore : IAuditingStore
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static SimpleLogAuditingStore Instance { get; } = new SimpleLogAuditingStore();

        public ILogger Logger { get; set; }

        public SimpleLogAuditingStore()
        {
            Logger = NullLogger.Instance;
        }

        public Task SaveAsync(AuditInfo auditInfo)
        {
            if (auditInfo.Exception == null)
            {
                Logger.LogInformation(auditInfo.ToString());
            }
            else
            {
                Logger.LogWarning(auditInfo.ToString());
            }

            return Task.FromResult(0);
        }

        public void Save(AuditInfo auditInfo)
        {
            if (auditInfo.Exception == null)
            {
                Logger.LogInformation(auditInfo.ToString());
            }
            else
            {
                Logger.LogWarning(auditInfo.ToString());
            }
        }
    }
}