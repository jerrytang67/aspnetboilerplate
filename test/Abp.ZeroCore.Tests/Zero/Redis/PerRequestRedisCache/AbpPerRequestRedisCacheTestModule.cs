using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Caching.Redis;

namespace Abp.Zero.Redis.PerRequestRedisCache;

[DependsOn(typeof(AbpAspNetCorePerRequestRedisCacheModule))]
public class AbpPerRequestRedisCacheTestModule : AbpModule {
    public override void ConfigureServices() {
        IocManager.RegisterAssemblyByConvention(typeof(AbpPerRequestRedisCacheTestModule).GetAssembly());

        Configuration.Caching.UseRedis();

    }

    public override void Initialize() {
    }
}