using Abp.Dependency;
using Autofac;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    public class GenericInjection_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Resolve_Generic_Types()
        {
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<MyClass>();
            iocMgr.Builder.RegisterGeneric(typeof(EmptyImplOne<>)).As(typeof(IEmpty<>));
            iocMgr.BuildContainer();

            var genericObj = LocalIocManager.Resolve<IEmpty<MyClass>>();
            genericObj.GenericArg.GetType().ShouldBe(typeof(MyClass));
        }

        public interface IEmpty<T> where T : class
        {
            T GenericArg { get; set; }
        }

        public class EmptyImplOne<T> : IEmpty<T> where T : class
        {
            public T GenericArg { get; set; }
        }

        public class MyClass
        {

        }
    }
}
