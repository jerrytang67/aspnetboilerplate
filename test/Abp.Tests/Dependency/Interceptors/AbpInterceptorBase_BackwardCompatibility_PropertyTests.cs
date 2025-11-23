using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency.Interceptors
{
    /// <summary>
    /// Property-based tests for AbpInterceptorBase backward compatibility.
    /// **Feature: autofac-migration, Property 8: Interceptor backward compatibility**
    /// **Validates: Requirements 13.2, 13.5**
    /// </summary>
    public class AbpInterceptorBase_BackwardCompatibility_PropertyTests
    {
        /// <summary>
        /// Property: For any existing interceptor implementation that inherits from AbpInterceptorBase,
        /// when the base class is migrated to use Autofac.Extras.DynamicProxy,
        /// the interceptor should continue to work without modification.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(InterceptorTestGenerators) }, MaxTest = 100)]
        public Property Interceptors_Should_Work_With_Synchronous_Methods(NonEmptyString input)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register interceptor wrapped in AsyncDeterminationInterceptor for Autofac compatibility
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new TestSyncInterceptor()))
                .Named<IInterceptor>("TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.RegisterType<TestService>()
                .As<ITestService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITestService>();

            // Act
            var result = service.ProcessSync(input.Get);

            // Assert
            return (result == $"[INTERCEPTED]{input.Get}[/INTERCEPTED]").ToProperty()
                .Label($"Expected intercepted result for input: {input.Get}");
        }

        /// <summary>
        /// Property: For any existing interceptor implementation that inherits from AbpInterceptorBase,
        /// async methods should be properly intercepted.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(InterceptorTestGenerators) }, MaxTest = 100)]
        public Property Interceptors_Should_Work_With_Asynchronous_Methods(NonEmptyString input)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register interceptor wrapped in AsyncDeterminationInterceptor for Autofac compatibility
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new TestAsyncInterceptor()))
                .Named<IInterceptor>("TestAsyncInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.RegisterType<TestService>()
                .As<ITestService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("TestAsyncInterceptor")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITestService>();

            // Act
            var result = service.ProcessAsync(input.Get).Result;

            // Assert
            return (result == $"[ASYNC]{input.Get}[/ASYNC]").ToProperty()
                .Label($"Expected async intercepted result for input: {input.Get}");
        }

        /// <summary>
        /// Property: For any existing interceptor implementation that inherits from AbpInterceptorBase,
        /// async methods with return values should be properly intercepted.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(InterceptorTestGenerators) }, MaxTest = 100)]
        public Property Interceptors_Should_Work_With_Asynchronous_Methods_With_Return_Value(PositiveInt value)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register interceptor wrapped in AsyncDeterminationInterceptor for Autofac compatibility
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new TestAsyncInterceptor()))
                .Named<IInterceptor>("TestAsyncInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.RegisterType<TestService>()
                .As<ITestService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("TestAsyncInterceptor")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITestService>();

            // Act
            var result = service.CalculateAsync(value.Get).Result;

            // Assert - interceptor adds 100 to the result
            return (result == value.Get + 100).ToProperty()
                .Label($"Expected {value.Get + 100} for input: {value.Get}");
        }

        /// <summary>
        /// Property: Multiple interceptors should work correctly on the same service.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(InterceptorTestGenerators) }, MaxTest = 100)]
        public Property Multiple_Interceptors_Should_Work_Together(NonEmptyString input)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register both interceptors wrapped in AsyncDeterminationInterceptor
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new TestSyncInterceptor()))
                .Named<IInterceptor>("TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new SecondInterceptor()))
                .Named<IInterceptor>("SecondInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.RegisterType<TestService>()
                .As<ITestService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy("SecondInterceptor", "TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITestService>();

            // Act
            var result = service.ProcessSync(input.Get);

            // Assert - both interceptors should be applied
            return (result == $"<SECOND>[INTERCEPTED]{input.Get}[/INTERCEPTED]</SECOND>").ToProperty()
                .Label($"Expected both interceptors applied for input: {input.Get}");
        }

        /// <summary>
        /// Property: Class interceptors should work correctly.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(InterceptorTestGenerators) }, MaxTest = 100)]
        public Property Class_Interceptors_Should_Work(NonEmptyString input)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register interceptor wrapped in AsyncDeterminationInterceptor
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(new TestSyncInterceptor()))
                .Named<IInterceptor>("TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.Builder.RegisterType<ConcreteTestService>()
                .EnableClassInterceptors()
                .InterceptedBy("TestSyncInterceptor")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ConcreteTestService>();

            // Act
            var result = service.ProcessSync(input.Get);

            // Assert
            return (result == $"[INTERCEPTED]{input.Get}[/INTERCEPTED]").ToProperty()
                .Label($"Expected class interceptor result for input: {input.Get}");
        }
    }

    #region Test Services and Interceptors

    public interface ITestService
    {
        string ProcessSync(string input);
        Task<string> ProcessAsync(string input);
        Task<int> CalculateAsync(int value);
    }

    public class TestService : ITestService
    {
        public virtual string ProcessSync(string input)
        {
            return input;
        }

        public virtual async Task<string> ProcessAsync(string input)
        {
            await Task.Delay(1);
            return input;
        }

        public virtual async Task<int> CalculateAsync(int value)
        {
            await Task.Delay(1);
            return value;
        }
    }

    public class ConcreteTestService
    {
        public virtual string ProcessSync(string input)
        {
            return input;
        }
    }

    /// <summary>
    /// Test interceptor that inherits from AbpInterceptorBase for synchronous methods.
    /// </summary>
    public class TestSyncInterceptor : AbpInterceptorBase
    {
        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"[INTERCEPTED]{str}[/INTERCEPTED]";
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
            var result = await (Task<TResult>)invocation.ReturnValue;
            return result;
        }
    }

    /// <summary>
    /// Test interceptor that inherits from AbpInterceptorBase for asynchronous methods.
    /// </summary>
    public class TestAsyncInterceptor : AbpInterceptorBase
    {
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
                return (TResult)(object)$"[ASYNC]{str}[/ASYNC]";
            }
            else if (result is int intValue)
            {
                return (TResult)(object)(intValue + 100);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Second test interceptor to verify multiple interceptors work together.
    /// </summary>
    public class SecondInterceptor : AbpInterceptorBase
    {
        public override void InterceptSynchronous(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"<SECOND>{str}</SECOND>";
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

    #region FsCheck Generators

    public class InterceptorTestGenerators
    {
        public static Arbitrary<NonEmptyString> NonEmptyStringArbitrary()
        {
            return Arb.Default.NonEmptyString()
                .Filter(s => !string.IsNullOrWhiteSpace(s.Get));
        }

        public static Arbitrary<PositiveInt> PositiveIntArbitrary()
        {
            return Arb.Default.PositiveInt();
        }
    }

    #endregion
}
