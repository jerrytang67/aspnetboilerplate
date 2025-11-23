using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency
{
    /// <summary>
    /// Property-based tests for container immutability after build.
    /// **Feature: autofac-migration, Property 4: Container immutability after build**
    /// **Validates: Requirements 11.1, 11.2, 11.4**
    /// </summary>
    public class IocManager_ContainerImmutability_PropertyTests
    {
        /// <summary>
        /// Property: For any service type and implementation type, attempting to register after container build should throw AbpException
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceTypeGenerators) })]
        public Property Registration_After_Build_Should_Always_Throw_Exception(ServiceRegistrationData data)
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act
            var exception = Record.Exception(() =>
            {
                if (data.ServiceType != null && data.ImplementationType != null)
                {
                    iocManager.Register(data.ServiceType, data.ImplementationType);
                }
                else if (data.ServiceType != null)
                {
                    iocManager.Register(data.ServiceType);
                }
            });

            // Assert - exception should be thrown and should be AbpException
            var isAbpException = exception != null && exception is AbpException;
            var hasCorrectMessage = exception?.Message.Contains("Cannot register services after container is built") == true;
            var mentionsConfigureServices = exception?.Message.Contains("ConfigureServices()") == true;

            return (isAbpException && hasCorrectMessage && mentionsConfigureServices)
                .ToProperty()
                .Label($"Registration of {data.ServiceType?.Name ?? "null"} should throw AbpException with clear message");
        }

        /// <summary>
        /// Property: For any list of service registrations, all should succeed before build and all should fail after build
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(ServiceTypeGenerators) })]
        public Property Multiple_Registrations_Should_Succeed_Before_Build_And_Fail_After(
            List<ServiceRegistrationData> registrations)
        {
            if (registrations == null || registrations.Count == 0)
            {
                return true.ToProperty();
            }

            // Filter to valid registrations only
            var validRegistrations = registrations
                .Where(r => r.ServiceType != null && r.ImplementationType != null)
                .Take(5) // Limit to 5 to keep test fast
                .ToList();

            if (validRegistrations.Count == 0)
            {
                return true.ToProperty();
            }

            // Arrange - Test before build
            var iocManagerBefore = new Abp.Dependency.IocManager();
            var allSucceededBefore = true;

            foreach (var reg in validRegistrations)
            {
                try
                {
                    iocManagerBefore.Register(reg.ServiceType, reg.ImplementationType);
                }
                catch
                {
                    allSucceededBefore = false;
                    break;
                }
            }

            // Arrange - Test after build
            var iocManagerAfter = new Abp.Dependency.IocManager();
            iocManagerAfter.BuildContainer();
            var allFailedAfter = true;

            foreach (var reg in validRegistrations)
            {
                try
                {
                    iocManagerAfter.Register(reg.ServiceType, reg.ImplementationType);
                    allFailedAfter = false; // Should have thrown
                    break;
                }
                catch (AbpException)
                {
                    // Expected
                }
                catch
                {
                    allFailedAfter = false; // Wrong exception type
                    break;
                }
            }

            return (allSucceededBefore && allFailedAfter)
                .ToProperty()
                .Label("All registrations should succeed before build and fail after build");
        }

        /// <summary>
        /// Property: BuildContainer can only be called once
        /// </summary>
        [Property(MaxTest = 100)]
        public Property BuildContainer_Can_Only_Be_Called_Once(PositiveInt callCount)
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();
            iocManager.BuildContainer();

            // Act - try to build again
            var exception = Record.Exception(() => iocManager.BuildContainer());

            // Assert
            var isAbpException = exception != null && exception is AbpException;
            var hasCorrectMessage = exception?.Message.Contains("already built") == true;
            var mentionsCannotRebuild = exception?.Message.Contains("cannot be rebuilt") == true;

            return (isAbpException && hasCorrectMessage && mentionsCannotRebuild)
                .ToProperty()
                .Label("Second call to BuildContainer should throw AbpException");
        }

        /// <summary>
        /// Property: IsContainerBuilt flag should accurately reflect container state
        /// </summary>
        [Property(MaxTest = 100)]
        public Property IsContainerBuilt_Should_Reflect_Container_State(bool shouldBuild)
        {
            // Arrange
            var iocManager = new Abp.Dependency.IocManager();

            // Act
            if (shouldBuild)
            {
                iocManager.BuildContainer();
            }

            // Assert
            return (iocManager.IsContainerBuilt == shouldBuild)
                .ToProperty()
                .Label($"IsContainerBuilt should be {shouldBuild} when container is {(shouldBuild ? "built" : "not built")}");
        }
    }

    /// <summary>
    /// Data class for service registration
    /// </summary>
    public class ServiceRegistrationData
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }

        public override string ToString()
        {
            return $"Service: {ServiceType?.Name ?? "null"}, Implementation: {ImplementationType?.Name ?? "null"}";
        }
    }

    /// <summary>
    /// Custom generators for service types
    /// </summary>
    public static class ServiceTypeGenerators
    {
        private static readonly Type[] TestServiceTypes = new[]
        {
            typeof(ITestService1),
            typeof(ITestService2),
            typeof(ITestService3),
            typeof(TestService1),
            typeof(TestService2),
            typeof(TestService3)
        };

        private static readonly Type[] TestImplementationTypes = new[]
        {
            typeof(TestService1),
            typeof(TestService2),
            typeof(TestService3)
        };

        public static Arbitrary<ServiceRegistrationData> ServiceRegistrationDataArbitrary()
        {
            return Arb.From(Gen.Elements(TestServiceTypes)
                .SelectMany(serviceType =>
                {
                    // For interfaces, pick a compatible implementation
                    if (serviceType.IsInterface)
                    {
                        var compatibleImpls = TestImplementationTypes
                            .Where(impl => serviceType.IsAssignableFrom(impl))
                            .ToArray();

                        if (compatibleImpls.Length > 0)
                        {
                            return Gen.Elements(compatibleImpls)
                                .Select(impl => new ServiceRegistrationData
                                {
                                    ServiceType = serviceType,
                                    ImplementationType = impl
                                });
                        }
                    }

                    // For concrete types, use self-registration
                    return Gen.Constant(new ServiceRegistrationData
                    {
                        ServiceType = serviceType,
                        ImplementationType = serviceType
                    });
                }));
        }

        // Test service interfaces and implementations
        public interface ITestService1 { }
        public interface ITestService2 { }
        public interface ITestService3 { }

        public class TestService1 : ITestService1 { }
        public class TestService2 : ITestService2 { }
        public class TestService3 : ITestService3 { }
    }
}
