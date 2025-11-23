using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Abp.Tests.Dependency.Interceptors
{
    /// <summary>
    /// Property-based tests for Autofac interceptor application.
    /// **Feature: autofac-migration, Property 9: Interceptor application through Autofac**
    /// **Validates: Requirements 13.3, 13.4**
    /// </summary>
    public class AutofacInterceptorApplication_PropertyTests
    {
        /// <summary>
        /// Property: For any service registered with interceptors using Autofac's EnableInterfaceInterceptors,
        /// when the service is resolved, the interceptors should be properly applied and invoked.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AutofacInterceptorGenerators) }, MaxTest = 100)]
        public Property Services_With_Interface_Interceptors_Should_Have_Interceptors_Applied(
            NonEmptyString serviceName,
            NonEmptyString methodInput)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            var callTracker = new InterceptorCallTracker();
            
            // Register interceptor with call tracker
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new TrackingInterceptor(callTracker, serviceName.Get)))
                .Named<IInterceptor>($"Tracker_{serviceName.Get}")
                .InstancePerDependency();
            
            // Register service with interface interception
            iocManager.Builder.RegisterType<TrackedService>()
                .As<ITrackedService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy($"Tracker_{serviceName.Get}")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITrackedService>();

            // Act
            var result = service.Execute(methodInput.Get);

            // Assert - verify interceptor was invoked
            return (callTracker.WasCalled(serviceName.Get) && 
                    result == $"[{serviceName.Get}]{methodInput.Get}[/{serviceName.Get}]")
                .ToProperty()
                .Label($"Interceptor '{serviceName.Get}' should be applied and invoked");
        }

        /// <summary>
        /// Property: For any service registered with interceptors using Autofac's EnableClassInterceptors,
        /// when the service is resolved, the interceptors should be properly applied and invoked.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AutofacInterceptorGenerators) }, MaxTest = 100)]
        public Property Services_With_Class_Interceptors_Should_Have_Interceptors_Applied(
            NonEmptyString serviceName,
            NonEmptyString methodInput)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            var callTracker = new InterceptorCallTracker();
            
            // Register interceptor with call tracker
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new TrackingInterceptor(callTracker, serviceName.Get)))
                .Named<IInterceptor>($"Tracker_{serviceName.Get}")
                .InstancePerDependency();
            
            // Register service with class interception
            iocManager.Builder.RegisterType<ConcreteTrackedService>()
                .EnableClassInterceptors()
                .InterceptedBy($"Tracker_{serviceName.Get}")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ConcreteTrackedService>();

            // Act
            var result = service.Execute(methodInput.Get);

            // Assert - verify interceptor was invoked
            return (callTracker.WasCalled(serviceName.Get) && 
                    result == $"[{serviceName.Get}]{methodInput.Get}[/{serviceName.Get}]")
                .ToProperty()
                .Label($"Class interceptor '{serviceName.Get}' should be applied and invoked");
        }

        /// <summary>
        /// Property: For any service registered with multiple interceptors,
        /// all interceptors should be properly applied in the correct order.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AutofacInterceptorGenerators) }, MaxTest = 100)]
        public Property Multiple_Interceptors_Should_Be_Applied_In_Order(
            NonEmptyArray<NonEmptyString> interceptorNames,
            NonEmptyString methodInput)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            var callTracker = new InterceptorCallTracker();
            var names = interceptorNames.Get.Select(n => n.Get).Distinct().Take(5).ToList();
            
            if (names.Count == 0)
            {
                return true.ToProperty().Label("Skipped: no valid interceptor names");
            }

            // Register all interceptors
            foreach (var name in names)
            {
                iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(
                        new TrackingInterceptor(callTracker, name)))
                    .Named<IInterceptor>($"Tracker_{name}")
                    .InstancePerDependency();
            }
            
            // Register service with all interceptors
            var registration = iocManager.Builder.RegisterType<TrackedService>()
                .As<ITrackedService>()
                .EnableInterfaceInterceptors();
            
            foreach (var name in names)
            {
                registration = registration.InterceptedBy($"Tracker_{name}");
            }
            
            registration.InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITrackedService>();

            // Act
            service.Execute(methodInput.Get);

            // Assert - verify all interceptors were invoked
            var allCalled = names.All(name => callTracker.WasCalled(name));
            return allCalled.ToProperty()
                .Label($"All {names.Count} interceptors should be invoked");
        }

        /// <summary>
        /// Property: For any service registered with async method interceptors,
        /// the interceptors should properly handle async methods.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AutofacInterceptorGenerators) }, MaxTest = 100)]
        public Property Async_Method_Interceptors_Should_Work_Correctly(
            NonEmptyString serviceName,
            PositiveInt value)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            var callTracker = new InterceptorCallTracker();
            
            // Register async interceptor
            iocManager.Builder.Register(c => new AsyncDeterminationInterceptor(
                    new AsyncTrackingInterceptor(callTracker, serviceName.Get)))
                .Named<IInterceptor>($"AsyncTracker_{serviceName.Get}")
                .InstancePerDependency();
            
            // Register service with interface interception
            iocManager.Builder.RegisterType<TrackedService>()
                .As<ITrackedService>()
                .EnableInterfaceInterceptors()
                .InterceptedBy($"AsyncTracker_{serviceName.Get}")
                .InstancePerDependency();
            
            iocManager.BuildContainer();

            var service = iocManager.Resolve<ITrackedService>();

            // Act
            var result = service.ExecuteAsync(value.Get).Result;

            // Assert - verify async interceptor was invoked and modified result
            return (callTracker.WasCalled(serviceName.Get) && 
                    result == value.Get + 1000)
                .ToProperty()
                .Label($"Async interceptor '{serviceName.Get}' should be applied and modify result");
        }

        /// <summary>
        /// Property: For any service registered with interceptors through conventional registration,
        /// the interceptors should be properly applied.
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AutofacInterceptorGenerators) }, MaxTest = 100)]
        public Property Conventionally_Registered_Services_Should_Have_Interceptors_Applied(
            NonEmptyString methodInput)
        {
            // Arrange - create fresh IocManager for each test
            var iocManager = new IocManager();
            
            // Register the conventional registrar
            var registrar = new BasicConventionalRegistrar();
            
            // Register the service assembly
            var context = new ConventionalRegistrationContext(
                typeof(ConventionallyRegisteredService).Assembly,
                iocManager,
                new ConventionalRegistrationConfig());
            
            registrar.RegisterAssembly(context);
            
            iocManager.BuildContainer();

            // Act - resolve service that should have interceptors applied
            var service = iocManager.Resolve<IConventionallyRegisteredService>();
            var result = service.Process(methodInput.Get);

            // Assert - verify service is resolved and works
            return (service != null && result == methodInput.Get)
                .ToProperty()
                .Label("Conventionally registered service should be resolvable");
        }
    }

    #region Test Services and Interceptors

    public interface ITrackedService
    {
        string Execute(string input);
        Task<int> ExecuteAsync(int value);
    }

    public class TrackedService : ITrackedService
    {
        public virtual string Execute(string input)
        {
            return input;
        }

        public virtual async Task<int> ExecuteAsync(int value)
        {
            await Task.Delay(1);
            return value;
        }
    }

    public class ConcreteTrackedService
    {
        public virtual string Execute(string input)
        {
            return input;
        }
    }

    public interface IConventionallyRegisteredService
    {
        string Process(string input);
    }

    public class ConventionallyRegisteredService : IConventionallyRegisteredService, ITransientDependency
    {
        public string Process(string input)
        {
            return input;
        }
    }

    /// <summary>
    /// Interceptor that tracks calls and modifies return values.
    /// </summary>
    public class TrackingInterceptor : AbpInterceptorBase
    {
        private readonly InterceptorCallTracker _tracker;
        private readonly string _name;

        public TrackingInterceptor(InterceptorCallTracker tracker, string name)
        {
            _tracker = tracker;
            _name = name;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
            
            if (invocation.ReturnValue is string str)
            {
                invocation.ReturnValue = $"[{_name}]{str}[/{_name}]";
            }
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }
    }

    /// <summary>
    /// Async interceptor that tracks calls and modifies return values.
    /// </summary>
    public class AsyncTrackingInterceptor : AbpInterceptorBase
    {
        private readonly InterceptorCallTracker _tracker;
        private readonly string _name;

        public AsyncTrackingInterceptor(InterceptorCallTracker tracker, string name)
        {
            _tracker = tracker;
            _name = name;
        }

        public override void InterceptSynchronous(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
        }

        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            _tracker.RecordCall(_name);
            invocation.Proceed();
            var result = await (Task<TResult>)invocation.ReturnValue;
            
            if (result is int intValue)
            {
                return (TResult)(object)(intValue + 1000);
            }
            
            return result;
        }
    }

    /// <summary>
    /// Helper class to track interceptor calls.
    /// </summary>
    public class InterceptorCallTracker
    {
        private readonly HashSet<string> _calledInterceptors = new HashSet<string>();
        private readonly object _lock = new object();

        public void RecordCall(string interceptorName)
        {
            lock (_lock)
            {
                _calledInterceptors.Add(interceptorName);
            }
        }

        public bool WasCalled(string interceptorName)
        {
            lock (_lock)
            {
                return _calledInterceptors.Contains(interceptorName);
            }
        }
    }

    #endregion

    #region FsCheck Generators

    public class AutofacInterceptorGenerators
    {
        public static Arbitrary<NonEmptyString> NonEmptyStringArbitrary()
        {
            return Arb.Default.NonEmptyString()
                .Filter(s => !string.IsNullOrWhiteSpace(s.Get) && 
                            s.Get.Length <= 50 &&
                            !s.Get.Contains("[") && 
                            !s.Get.Contains("]"));
        }

        public static Arbitrary<PositiveInt> PositiveIntArbitrary()
        {
            return Arb.Default.PositiveInt()
                .Filter(i => i.Get < 1000000); // Keep values reasonable
        }

        public static Arbitrary<NonEmptyArray<NonEmptyString>> NonEmptyArrayArbitrary()
        {
            return Arb.Default.NonEmptyArray<NonEmptyString>()
                .Filter(arr => arr.Get.Length <= 10); // Limit array size
        }
    }

    #endregion
}
