using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Modules;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Abp.AspNetCore.Tests.ServiceRegistration
{
    /// <summary>
    /// Property-based tests for service registration completeness in AddAbp flow.
    /// **Feature: autofac-migration, Property 3: Service registration before container build**
    /// </summary>
    public class AbpServiceCollectionExtensions_ServiceRegistration_PropertyTests
    {
        /// <summary>
        /// Property 3: Service registration before container build
        /// For any module, when ConfigureServices is called, all service registrations should complete 
        /// before the container is built, and all registered services should be resolvable after container construction.
        /// **Validates: Requirements 4.5**
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceRegistrationGenerators) })]
        public Property Services_Registered_In_ConfigureServices_Should_Be_Resolvable_After_Build(
            List<ServiceRegistration> serviceRegistrations)
        {
            // Filter out invalid registrations
            var validRegistrations = serviceRegistrations
                .Where(sr => sr != null && sr.ServiceType != null && sr.ImplementationType != null)
                .Take(10) // Limit to 10 services to keep tests fast
                .ToList();

            if (validRegistrations.Count == 0)
            {
                return true.ToProperty();
            }

            try
            {
                // Arrange: Create a fresh IocManager for each test run
                var iocManager = new IocManager();
                var services = new ServiceCollection();

                // Create a test module type dynamically to avoid reuse issues
                var testModuleType = typeof(PropertyTestModule);
                
                // Act: Call AddAbp which should:
                // 1. Call ConfigureServices on all modules
                // 2. Build the container
                // 3. Return a service provider
                IServiceProvider serviceProvider = null;
                
                // Store registrations in a static field for the module to access
                PropertyTestModule.CurrentRegistrations = validRegistrations;
                
                serviceProvider = services.AddAbp<PropertyTestModule>(options =>
                {
                    options.IocManager = iocManager;
                });

                // Assert: All services registered in ConfigureServices should be resolvable
                var allServicesResolvable = validRegistrations.All(sr =>
                {
                    try
                    {
                        var service = serviceProvider.GetService(sr.ServiceType);
                        return service != null && sr.ServiceType.IsInstanceOfType(service);
                    }
                    catch
                    {
                        return false;
                    }
                });

                // Cleanup
                PropertyTestModule.CurrentRegistrations = null;
                (serviceProvider as IDisposable)?.Dispose();

                return allServicesResolvable.ToProperty();
            }
            catch (Exception ex)
            {
                // If there's an exception during the test, log it and fail
                Console.WriteLine($"Test failed with exception: {ex.Message}");
                PropertyTestModule.CurrentRegistrations = null;
                return false.ToProperty();
            }
        }

        /// <summary>
        /// Test module that registers services in ConfigureServices
        /// </summary>
        private class PropertyTestModule : AbpModule
        {
            // Static field to pass registrations to the module instance
            public static List<ServiceRegistration> CurrentRegistrations { get; set; }

            public PropertyTestModule()
            {
            }

            public override void ConfigureServices()
            {
                var registrations = CurrentRegistrations ?? new List<ServiceRegistration>();
                
                foreach (var registration in registrations)
                {
                    try
                    {
                        IocManager.Register(
                            registration.ServiceType,
                            registration.ImplementationType,
                            registration.LifeStyle);
                    }
                    catch
                    {
                        // Ignore registration errors in property tests
                        // (e.g., duplicate registrations, invalid types)
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a service registration for property testing
    /// </summary>
    public class ServiceRegistration
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public DependencyLifeStyle LifeStyle { get; set; }

        public ServiceRegistration(Type serviceType, Type implementationType, DependencyLifeStyle lifeStyle)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            LifeStyle = lifeStyle;
        }
    }

    /// <summary>
    /// FsCheck generators for service registrations
    /// </summary>
    public class ServiceRegistrationGenerators
    {
        // Test service interfaces
        public interface ITestService1 { }
        public interface ITestService2 { }
        public interface ITestService3 { }
        public interface ITestService4 { }
        public interface ITestService5 { }

        // Test service implementations
        public class TestService1 : ITestService1 { }
        public class TestService2 : ITestService2 { }
        public class TestService3 : ITestService3 { }
        public class TestService4 : ITestService4 { }
        public class TestService5 : ITestService5 { }

        private static readonly List<(Type ServiceType, Type ImplementationType)> ServicePairs = new()
        {
            (typeof(ITestService1), typeof(TestService1)),
            (typeof(ITestService2), typeof(TestService2)),
            (typeof(ITestService3), typeof(TestService3)),
            (typeof(ITestService4), typeof(TestService4)),
            (typeof(ITestService5), typeof(TestService5))
        };

        public static Arbitrary<List<ServiceRegistration>> ServiceRegistrationListArbitrary()
        {
            return Arb.From(
                from count in Gen.Choose(0, 5)
                from registrations in Gen.ListOf(count, ServiceRegistrationGen())
                select registrations.ToList()
            );
        }

        private static Gen<ServiceRegistration> ServiceRegistrationGen()
        {
            return from pairArray in Gen.Elements(ServicePairs.ToArray())
                   from lifeStyle in Gen.Elements(DependencyLifeStyle.Singleton, DependencyLifeStyle.Transient)
                   select new ServiceRegistration(pairArray.ServiceType, pairArray.ImplementationType, lifeStyle);
        }
    }
}
