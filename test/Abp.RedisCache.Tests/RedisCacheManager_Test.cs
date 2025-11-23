using System;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Runtime.Caching;
using Abp.Runtime.Caching.Configuration;
using Abp.Runtime.Caching.Redis;
using Abp.Tests;
using Autofac;
using NSubstitute;
using Xunit;
using Shouldly;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace Abp.RedisCache.Tests
{
    public class RedisCacheManager_Test : TestBaseWithLocalIocManager
    {
        private readonly ITypedCache<string, MyCacheItem> _cache;
        private IAbpRedisCacheDatabaseProvider _redisDatabaseProvider;
        private IRedisCacheSerializer _redisSerializer;
        private IDatabase _redisDatabase;

        public RedisCacheManager_Test()
        {
            LocalIocManager.Register<AbpRedisCacheOptions>();
            LocalIocManager.Register<ICachingConfiguration, CachingConfiguration>();

            _redisDatabase = Substitute.For<IDatabase>();
            _redisDatabaseProvider = Substitute.For<IAbpRedisCacheDatabaseProvider>();
            _redisDatabaseProvider.GetDatabase().Returns(_redisDatabase);

            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterInstance(_redisDatabaseProvider).As<IAbpRedisCacheDatabaseProvider>().SingleInstance();

            LocalIocManager.Register<ICacheManager, AbpRedisCacheManager>();
            LocalIocManager.Register<IRedisCacheSerializer, DefaultRedisCacheSerializer>();

            iocMgr.Builder.RegisterInstance(Substitute.For<IAbpStartupConfiguration>()).As<IAbpStartupConfiguration>();

            LocalIocManager.Register<IAbpRedisCacheKeyNormalizer, AbpRedisCacheKeyNormalizer>();
            LocalIocManager.Register<IOptions<AbpRedisCacheOptions>, OptionsWrapper<AbpRedisCacheOptions>>();
            LocalIocManager.Register<IMultiTenancyConfig, MultiTenancyConfig>();
            
            // Register AbpRedisCache as transient (required for cache creation)
            LocalIocManager.Register<AbpRedisCache>(DependencyLifeStyle.Transient);

            iocMgr.BuildContainer();

            _redisSerializer = LocalIocManager.Resolve<IRedisCacheSerializer>();

            LocalIocManager.Resolve<ICachingConfiguration>().Configure("MyTestCacheItems", cache =>
            {
                cache.DefaultSlidingExpireTime = TimeSpan.FromHours(24);
            });

            _cache = LocalIocManager.Resolve<ICacheManager>().GetCache<string, MyCacheItem>("MyTestCacheItems");
        }

        [Fact]
        public void Cache_Options_Configuration_Test()
        {
            _cache.DefaultSlidingExpireTime.ShouldBe(TimeSpan.FromHours(24));
        }

        [Theory]
        [InlineData("A", 42)]
        [InlineData("B", 43)]
        public void Simple_Get_Set_Test(string cacheKey, int cacheValue)
        {
            var item = _cache.Get(cacheKey, () => new MyCacheItem { Value = cacheValue });

            item.ShouldNotBe(null);
            item.Value.ShouldBe(cacheValue);

            var cachedObject = _redisSerializer.Serialize(new MyCacheItem { Value = cacheValue }, typeof(MyCacheItem));
            _redisDatabase.StringGet(Arg.Any<RedisKey>()).Returns(cachedObject);
            _redisDatabase.Received().StringGet(Arg.Any<RedisKey>());

            _cache.GetOrDefault(cacheKey).Value.ShouldBe(cacheValue);
        }

        [Serializable]
        public class MyCacheItem
        {
            public int Value { get; set; }
        }
    }
}
