using System;
using System.Reflection;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace Abp.Runtime.Caching.Redis
{
    /// <summary>
    /// This modules is used to replace ABP's cache system with Redis server.
    /// </summary>
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpRedisCacheModule : AbpModule
    {
        public override void ConfigureServices()
        {
            // Register Redis cache configuration options
            IocManager.Register<AbpRedisCacheOptions>();
            
            // Register AbpRedisCache as transient (created per cache name)
            IocManager.Register<AbpRedisCache>(DependencyLifeStyle.Transient);
            IocManager.RegisterAssemblyByConvention(typeof(AbpRedisCacheModule).GetAssembly());
        }


        public override void Initialize()
        {
            // Register assembly by convention (uses resolved services)
        }
    }
}
