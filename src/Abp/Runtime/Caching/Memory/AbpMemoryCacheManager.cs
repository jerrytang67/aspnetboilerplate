using System.Linq;
using Microsoft.Extensions.Logging;
using Abp.Dependency;
using Abp.Runtime.Caching.Configuration;
using Abp.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Abp.Runtime.Caching.Memory {
    /// <summary>
    /// Implements <see cref="ICacheManager"/> to work with MemoryCache.
    /// </summary>
    public class AbpMemoryCacheManager : CacheManagerBase<ICache>, ICacheManager {
        private readonly IIocManager _iocManager;
        public ILogger Logger { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AbpMemoryCacheManager(ICachingConfiguration configuration, IIocManager iocManager)
            : base(configuration) {
            _iocManager = iocManager;
            Logger = iocManager.Resolve<ILoggerFactory>().CreateLogger(typeof(AbpMemoryCacheManager));
        }

        protected override ICache CreateCacheImplementation(string name) {
            return new AbpMemoryCache(name, _iocManager, Configuration?.AbpConfiguration?.Caching?.MemoryCacheOptions) {
            };
        }

        protected override void DisposeCaches() {
            foreach (var cache in Caches.Values) {
                cache.Dispose();
            }
        }
    }
}