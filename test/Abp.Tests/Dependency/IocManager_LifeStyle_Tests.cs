using Abp.Dependency;
using Autofac;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    public class IocManager_LifeStyle_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Call_Dispose_Of_Transient_Dependency_When_IocManager_Is_Disposed()
        {
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<SimpleDisposableObject>().InstancePerDependency();
            iocMgr.BuildContainer();

            var obj = LocalIocManager.Resolve<SimpleDisposableObject>();

            LocalIocManager.Dispose();

            obj.DisposeCount.ShouldBe(1);
        }

        [Fact]
        public void Should_Call_Dispose_Of_Singleton_Dependency_When_IocManager_Is_Disposed()
        {
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<SimpleDisposableObject>().SingleInstance();
            iocMgr.BuildContainer();

            var obj = LocalIocManager.Resolve<SimpleDisposableObject>();

            LocalIocManager.Dispose();

            obj.DisposeCount.ShouldBe(1);
        }
    }
}
