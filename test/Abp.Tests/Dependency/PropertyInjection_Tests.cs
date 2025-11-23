using Abp.Application.Services;
using Abp.Dependency;
using Abp.Runtime.Session;
using Autofac;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    public class PropertyInjection_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Inject_Session_For_ApplicationService()
        {
            var session = Substitute.For<IAbpSession>();
            session.TenantId.Returns(1);
            session.UserId.Returns(42);

            LocalIocManager.Register<MyApplicationService>();

            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterInstance(session).As<IAbpSession>();
            iocMgr.BuildContainer();

            var myAppService = LocalIocManager.Resolve<MyApplicationService>();
            myAppService.TestSession();
        }

        private class MyApplicationService : ApplicationService
        {
            public void TestSession()
            {
                AbpSession.ShouldNotBe(null);
                AbpSession.TenantId.ShouldBe(1);
                AbpSession.UserId.ShouldBe(42);
            }
        }
    }
}
