using System;
using Microsoft.Extensions.Logging;
using Abp.Dependency;
using Abp.Logging;

namespace Abp.Web.Security.AntiForgery {
    public class AbpAntiForgeryManager(IAbpAntiForgeryConfiguration configuration, ILogger<AbpAntiForgeryManager> logger)
        : IAbpAntiForgeryManager, IAbpAntiForgeryValidator, ITransientDependency {
        public ILogger Logger { protected get; set; } = logger;
        public IAbpAntiForgeryConfiguration Configuration { get; } = configuration;

        public virtual string GenerateToken() {
            return Guid.NewGuid().ToString("D");
        }

        public virtual bool IsValid(string cookieValue, string tokenValue) {
            return cookieValue == tokenValue;
        }
    }
}