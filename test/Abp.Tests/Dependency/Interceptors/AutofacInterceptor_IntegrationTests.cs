using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency.Interceptors
{
    /// <summary>
    /// Integration tests for Autofac interceptor functionality.
    /// Tests interface interception, class interception, multiple interceptors, ordering, and async methods.
    /// **Validates: Requirements 13.3, 13.4, 13.5**
    /// </summary>
    public class AutofacInterceptor_IntegrationTests : TestBaseWithLocalIocManager
    {
        /// <summary>
        /// Test that interface interception works correctly with Autofac.
        /// </summary>
        [Fact]
        public void Interface_Interception_Should_Work_Correctly()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new PrefixInterceptor("PREFIX")))
                .Named<IInterceptor>("PrefixInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("PrefixInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            var result = service.Greet("World");

            // Assert
            result.ShouldBe("PREFIX: Hello World");
        }

        /// <summary>
        /// Test that class interception works correctly with Autofac.
        /// </summary>
        [Fact]
        public void Class_Interception_Should_Work_Correctly()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new SuffixInterceptor("SUFFIX")))
                .Named<IInterceptor>("SuffixInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<ConcreteGreetingService>()
                .EnableClassInterceptors()
                .InterceptedBy("SuffixInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<ConcreteGreetingService>();
            var result = service.Greet("World");

            // Assert
            result.ShouldBe("Hello World :SUFFIX");
        }

        /// <summary>
        /// Test that multiple interceptors work correctly on the same service.
        /// </summary>
        [Fact]
        public void Multiple_Interceptors_Should_Work_On_Same_Service()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new PrefixInterceptor("FIRST")))
                .Named<IInterceptor>("FirstInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new SuffixInterceptor("LAST")))
                .Named<IInterceptor>("LastInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("FirstInterceptor", "LastInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            var result = service.Greet("World");

            // Assert
            // First interceptor adds prefix, then last interceptor adds suffix
            result.ShouldBe("FIRST: Hello World :LAST");
        }

        /// <summary>
        /// Test that interceptor ordering is respected.
        /// </summary>
        [Fact]
        public void Interceptor_Ordering_Should_Be_Respected()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            var executionOrder = new List<string>();
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new OrderTrackingInterceptor(executionOrder, "First")))
                .Named<IInterceptor>("First")
                .InstancePerDependency();
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new OrderTrackingInterceptor(executionOrder, "Second")))
                .Named<IInterceptor>("Second")
                .InstancePerDependency();
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new OrderTrackingInterceptor(executionOrder, "Third")))
                .Named<IInterceptor>("Third")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("First", "Second", "Third")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            service.Greet("Test");

            // Assert
            executionOrder.Count.ShouldBe(6); // 3 before + 3 after
            executionOrder[0].ShouldBe("First-Before");
            executionOrder[1].ShouldBe("Second-Before");
            executionOrder[2].ShouldBe("Third-Before");
            executionOrder[3].ShouldBe("Third-After");
            executionOrder[4].ShouldBe("Second-After");
            executionOrder[5].ShouldBe("First-After");
        }

        /// <summary>
        /// Test that async method interception works correctly.
        /// </summary>
        [Fact]
        public async Task Async_Method_Interception_Should_Work_Correctly()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new AsyncPrefixInterceptor("ASYNC")))
                .Named<IInterceptor>("AsyncInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("AsyncInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            var result = await service.GreetAsync("World");

            // Assert
            result.ShouldBe("ASYNC: Hello World");
        }

        /// <summary>
        /// Test that async method interception with return value works correctly.
        /// </summary>
        [Fact]
        public async Task Async_Method_With_Return_Value_Interception_Should_Work_Correctly()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new AsyncMultiplierInterceptor(2)))
                .Named<IInterceptor>("MultiplierInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<CalculatorService>()
                .As<ICalculatorService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("MultiplierInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<ICalculatorService>();
            var result = await service.AddAsync(5, 3);

            // Assert
            result.ShouldBe(16); // (5 + 3) * 2
        }

        /// <summary>
        /// Test that interceptors can access and modify method parameters.
        /// </summary>
        [Fact]
        public void Interceptors_Should_Access_Method_Parameters()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new ParameterModifyingInterceptor()))
                .Named<IInterceptor>("ParamInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("ParamInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            var result = service.Greet("world");

            // Assert
            // Interceptor should uppercase the parameter
            result.ShouldBe("Hello WORLD");
        }

        /// <summary>
        /// Test that interceptors work with methods that throw exceptions.
        /// </summary>
        [Fact]
        public void Interceptors_Should_Handle_Exceptions_Correctly()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            var exceptionCaught = false;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new ExceptionHandlingInterceptor(() => exceptionCaught = true)))
                .Named<IInterceptor>("ExceptionInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("ExceptionInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act & Assert
            var service = LocalIocManager.Resolve<IGreetingService>();
            Should.Throw<InvalidOperationException>(() => service.ThrowException());
            exceptionCaught.ShouldBeTrue();
        }

        /// <summary>
        /// Test that interceptors can be resolved with dependencies.
        /// </summary>
        [Fact]
        public void Interceptors_Should_Support_Dependency_Injection()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            // Register a dependency for the interceptor
            iocMgr.Builder.RegisterType<PrefixProvider>()
                .As<IPrefixProvider>()
                .SingleInstance();
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new DependencyInjectedInterceptor(c.Resolve<IPrefixProvider>())))
                .Named<IInterceptor>("DIInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GreetingService>()
                .As<IGreetingService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("DIInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGreetingService>();
            var result = service.Greet("World");

            // Assert
            result.ShouldBe("INJECTED: Hello World");
        }

        /// <summary>
        /// Test that interceptors work with generic methods.
        /// </summary>
        [Fact]
        public void Interceptors_Should_Work_With_Generic_Methods()
        {
            // Arrange
            var iocMgr = (IocManager)LocalIocManager;
            
            iocMgr.Builder.Register(c => new AsyncDeterminationInterceptor(new GenericMethodInterceptor()))
                .Named<IInterceptor>("GenericInterceptor")
                .InstancePerDependency();
            
            iocMgr.Builder.RegisterType<GenericService>()
                .As<IGenericService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("GenericInterceptor")
                .InstancePerDependency();
            
            iocMgr.BuildContainer();

            // Act
            var service = LocalIocManager.Resolve<IGenericService>();
            var stringResult = service.Process("test");
            var intResult = service.Process(42);

            // Assert
            stringResult.ShouldBe("PROCESSED: test");
            intResult.ShouldBe(42);
        }
    }

    #region Test Services

    public interface IGreetingService
    {
        string Greet(string name);
        Task<string> GreetAsync(string name);
        void ThrowException();
    }

    public class GreetingService : IGreetingService
    {
        public virtual string Greet(string name)
        {
            return $"Hello {name}";
        }

        public virtual async Task<string> GreetAsync(string name)
        {
            await Task.Delay(1);
            return $"Hello {name}";
        }

        public virtual void ThrowException()
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    public class ConcreteGreetingService
    {
        public virtual string Greet(string name)
        {
            return $"Hello {name}";
        }
    }

    public interface ICalculatorService
    {
        Task<int> AddAsync(int a, int b);
    }

    public class CalculatorService : ICalculatorService
    {
        public virtual async Task<int> AddAsync(int a, int b)
        {
            await Task.Delay(1);
            return a + b;
        }
    }

    public interface IGenericService
    {
        T Process<T>(T input);
    }

    public class GenericService : IGenericService
    {
        public virtual T Process<T>(T input)
        {
            return input;
        }
    }

    public interface IPrefixProvider
    {
        string GetPrefix();
    }

    public class PrefixProvider : IPrefixProvider
    {
        public string GetPrefix()
        {
            return "INJECTED";
        }
    }

    #endregion

    #region Test Interceptors

    public class PrefixInterceptor : AbpInterceptorBase
    {
        private readonly string _prefix;

        public PrefixInterceptor(string prefix)
        {
            _prefix = prefix;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"{_prefix}: {str}";
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    public class SuffixInterceptor : AbpInterceptorBase
    {
        private readonly string _suffix;

        public SuffixInterceptor(string suffix)
        {
            _suffix = suffix;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"{str} :{_suffix}";
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    public class OrderTrackingInterceptor : AbpInterceptorBase
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;

        public OrderTrackingInterceptor(List<string> executionOrder, string name)
        {
            _executionOrder = executionOrder;
            _name = name;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            _executionOrder.Add($"{_name}-Before");
            invocation.Proceed();
            _executionOrder.Add($"{_name}-After");
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            _executionOrder.Add($"{_name}-Before");
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
            _executionOrder.Add($"{_name}-After");
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            _executionOrder.Add($"{_name}-Before");
            invocation.Proceed();
            var result = await (Task<TResult>)invocation.ReturnValue;
            _executionOrder.Add($"{_name}-After");
            return result;
        }
    }

    public class AsyncPrefixInterceptor : AbpInterceptorBase
    {
        private readonly string _prefix;

        public AsyncPrefixInterceptor(string prefix)
        {
            _prefix = prefix;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            var result = await (Task<TResult>)invocation.ReturnValue;
            
            if (result is string str)
            {
                return (TResult)(object)$"{_prefix}: {str}";
            }
            
            return result;
        }
    }

    public class AsyncMultiplierInterceptor : AbpInterceptorBase
    {
        private readonly int _multiplier;

        public AsyncMultiplierInterceptor(int multiplier)
        {
            _multiplier = multiplier;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            var result = await (Task<TResult>)invocation.ReturnValue;
            
            if (result is int intValue)
            {
                return (TResult)(object)(intValue * _multiplier);
            }
            
            return result;
        }
    }

    public class ParameterModifyingInterceptor : AbpInterceptorBase
    {
        public override void InterceptSynchronous(IInvocation invocation)
        {
            // Modify parameters before proceeding
            if (invocation.Arguments.Length > 0 && invocation.Arguments[0] is string str)
            {
                invocation.Arguments[0] = str.ToUpper();
            }
            
            invocation.Proceed();
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    public class ExceptionHandlingInterceptor : AbpInterceptorBase
    {
        private readonly Action _onException;

        public ExceptionHandlingInterceptor(Action onException)
        {
            _onException = onException;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
            }
            catch
            {
                _onException();
                throw;
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
                await (Task)invocation.ReturnValue;
            }
            catch
            {
                _onException();
                throw;
            }
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
                return await (Task<TResult>)invocation.ReturnValue;
            }
            catch
            {
                _onException();
                throw;
            }
        }
    }

    public class DependencyInjectedInterceptor : AbpInterceptorBase
    {
        private readonly IPrefixProvider _prefixProvider;

        public DependencyInjectedInterceptor(IPrefixProvider prefixProvider)
        {
            _prefixProvider = prefixProvider;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"{_prefixProvider.GetPrefix()}: {str}";
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    public class GenericMethodInterceptor : AbpInterceptorBase
    {
        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"PROCESSED: {str}";
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    #endregion
}
