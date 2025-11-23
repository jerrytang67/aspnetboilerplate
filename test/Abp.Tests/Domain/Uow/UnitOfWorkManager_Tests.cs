using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Autofac;
using NSubstitute;
using Xunit;

namespace Abp.Tests.Domain.Uow
{
    public class UnitOfWorkManager_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Call_Uow_Methods()
        {
            var fakeUow = Substitute.For<IUnitOfWork>();

            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<UnitOfWorkDefaultOptions>().As<IUnitOfWorkDefaultOptions>().SingleInstance();
            iocMgr.Builder.RegisterType<UnitOfWorkManager>().As<IUnitOfWorkManager>().SingleInstance();
            iocMgr.Builder.RegisterInstance(fakeUow).As<IUnitOfWork>().SingleInstance();
            iocMgr.Builder.RegisterType<AsyncLocalCurrentUnitOfWorkProvider>().As<ICurrentUnitOfWorkProvider>().SingleInstance();
            iocMgr.BuildContainer();

            var uowManager = LocalIocManager.Resolve<IUnitOfWorkManager>();

            //Starting the first uow
            using (var uow1 = uowManager.Begin())
            {
                //so, begin will be called
                fakeUow.Received(1).Begin(Arg.Any<UnitOfWorkOptions>());

                //trying to begin a uow (not starting a new one, using the outer)
                using (var uow2 = uowManager.Begin())
                {
                    //Since there is a current uow, begin is not called
                    fakeUow.Received(1).Begin(Arg.Any<UnitOfWorkOptions>());

                    uow2.Complete();

                    //complete has no effect since outer uow should complete it
                    fakeUow.DidNotReceive().Complete();
                }

                //trying to begin a uow (forcing to start a NEW one)
                using (var uow2 = uowManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    //So, begin is called again to create an inner uow
                    fakeUow.Received(2).Begin(Arg.Any<UnitOfWorkOptions>());

                    uow2.Complete();

                    //And the inner uow should be completed
                    fakeUow.Received(1).Complete();
                }

                //complete the outer uow
                uow1.Complete();
            }

            fakeUow.Received(2).Complete();
            fakeUow.Received(2).Dispose();
        }
    }
}
