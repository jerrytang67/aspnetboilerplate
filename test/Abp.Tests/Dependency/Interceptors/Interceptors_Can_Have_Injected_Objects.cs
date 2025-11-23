using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency.Interceptors
{
    public class Interceptors_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Interceptors_Should_Work()
        {
            var iocMgr = (IocManager)LocalIocManager;
            iocMgr.Builder.RegisterType<BracketInterceptor>().InstancePerDependency();
            iocMgr.Builder.RegisterType<MyGreetingClass>()
                .EnableClassInterceptors()
                .InterceptedBy(typeof(BracketInterceptor))
                .InstancePerDependency();
            iocMgr.BuildContainer();

            var greetingObj = LocalIocManager.Resolve<MyGreetingClass>();

            greetingObj.SayHello("Halil").ShouldBe("(Hello Halil)");
        }

        public class MyGreetingClass
        {
            public virtual string SayHello(string name)
            {
                return "Hello " + name;
            }
        }

        public class BracketInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
                invocation.ReturnValue = "(" + invocation.ReturnValue + ")";
            }
        }
    }
}
