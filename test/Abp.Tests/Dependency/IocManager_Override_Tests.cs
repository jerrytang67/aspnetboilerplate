using System.Linq;
using Abp.Dependency;
using Autofac;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    public class IocManager_Override_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Not_Override_As_Default()
        {
            //Arrange
            LocalIocManager.Register<IMyService, MyImpl1>(DependencyLifeStyle.Transient);
            LocalIocManager.Register<IMyService, MyImpl2>(DependencyLifeStyle.Transient);
            LocalIocManager.Register<IMyService, MyImpl3>(DependencyLifeStyle.Transient);

            //Act
            var service = LocalIocManager.Resolve<IMyService>();
            var allServices = LocalIocManager.ResolveAll<IMyService>();

            //Assert
            service.ShouldBeOfType<MyImpl1>();
            allServices.Length.ShouldBe(3);
            allServices.Any(s => s.GetType() == typeof(MyImpl1)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl2)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl3)).ShouldBeTrue();
        }

        [Fact]
        public void Should_Override_When_Using_Last_Registration()
        {
            //Arrange
            // In Autofac, later registrations override earlier ones unless PreserveExistingDefaults is used
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<MyImpl1>().As<IMyService>().InstancePerDependency().PreserveExistingDefaults();
            iocMgr.Builder.RegisterType<MyImpl2>().As<IMyService>().InstancePerDependency();
            iocMgr.BuildContainer();

            //Act
            var service = LocalIocManager.Resolve<IMyService>();
            var allServices = LocalIocManager.ResolveAll<IMyService>();

            //Assert
            service.ShouldBeOfType<MyImpl2>();
            allServices.Length.ShouldBe(2);
            allServices.Any(s => s.GetType() == typeof(MyImpl1)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl2)).ShouldBeTrue();
        }

        [Fact]
        public void Should_Override_When_Using_Last_Registration_Multiple()
        {
            //Arrange
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<MyImpl1>().As<IMyService>().InstancePerDependency().PreserveExistingDefaults();
            iocMgr.Builder.RegisterType<MyImpl2>().As<IMyService>().InstancePerDependency().PreserveExistingDefaults();
            iocMgr.Builder.RegisterType<MyImpl3>().As<IMyService>().InstancePerDependency();
            iocMgr.BuildContainer();

            //Act
            var service = LocalIocManager.Resolve<IMyService>();
            var allServices = LocalIocManager.ResolveAll<IMyService>();

            //Assert
            service.ShouldBeOfType<MyImpl3>();
            allServices.Length.ShouldBe(3);
            allServices.Any(s => s.GetType() == typeof(MyImpl1)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl2)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl3)).ShouldBeTrue();
        }

        [Fact]
        public void Should_Get_Specific_Default_Service()
        {
            //Arrange
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<MyImpl1>().As<IMyService>().InstancePerDependency().PreserveExistingDefaults();
            iocMgr.Builder.RegisterType<MyImpl2>().As<IMyService>().InstancePerDependency();
            iocMgr.Builder.RegisterType<MyImpl3>().As<IMyService>().InstancePerDependency().PreserveExistingDefaults();
            iocMgr.BuildContainer();

            //Act
            var service = LocalIocManager.Resolve<IMyService>();
            var allServices = LocalIocManager.ResolveAll<IMyService>();

            //Assert
            service.ShouldBeOfType<MyImpl2>();
            allServices.Length.ShouldBe(3);
            allServices.Any(s => s.GetType() == typeof(MyImpl1)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl2)).ShouldBeTrue();
            allServices.Any(s => s.GetType() == typeof(MyImpl3)).ShouldBeTrue();
        }

        public class MyImpl1 : IMyService
        {

        }

        public class MyImpl2 : IMyService
        {

        }

        public class MyImpl3 : IMyService
        {

        }

        public interface IMyService
        {
        }
    }
}
