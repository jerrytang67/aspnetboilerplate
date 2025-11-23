using System.Collections.Generic;
using Abp.Dependency;
using Abp.Modules;
using Abp.Runtime.Caching.Redis;
using Abp.TestBase;
using Autofac;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using StackExchange.Redis;

namespace Abp.Zero.Redis.PerRequestRedisCache;

public abstract class PerRequestRedisCacheTestsBase<TStartupModule> : AbpIntegratedTestBase<TStartupModule>
where TStartupModule : AbpModule
{
    protected IDatabase RedisDatabase;
    protected IRedisCacheSerializer RedisSerializer;
    protected HttpContext CurrentHttpContext;

    protected override void PreInitialize()
    {
        CurrentHttpContext = GetNewContextSubstitute();

        RedisDatabase = Substitute.For<IDatabase>();

        var redisDatabaseProvider = Substitute.For<IAbpRedisCacheDatabaseProvider>();
        redisDatabaseProvider.GetDatabase().Returns(RedisDatabase);

        var iocMgr = (IocManager)LocalIocManager;
        iocMgr.Builder.RegisterInstance(redisDatabaseProvider)
            .As<IAbpRedisCacheDatabaseProvider>()
            .SingleInstance();
    }

    protected PerRequestRedisCacheTestsBase()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(info => CurrentHttpContext);

        var iocMgr = (IocManager)LocalIocManager;
        iocMgr.Builder.RegisterInstance(httpContextAccessor)
            .As<IHttpContextAccessor>()
            .SingleInstance();

        RedisSerializer = LocalIocManager.Resolve<IRedisCacheSerializer>();
    }

    protected HttpContext GetNewContextSubstitute()
    {
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Items = new Dictionary<object, object>();
        return httpContext;
    }

    protected void ChangeHttpContext()
    {
        CurrentHttpContext = GetNewContextSubstitute();
    }
}