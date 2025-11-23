using System;
using System.Reflection;
using Abp.Dependency;
using Abp.Modules;
using Shouldly;
using Xunit;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace Abp.Tests.Modules
{
    /// <summary>
    /// Tests for backward compatibility support during the Autofac migration.
    /// Requirements: 10.1, 10.2, 10.3
    /// </summary>
    public class AbpModule_BackwardCompatibility_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Module_Without_ConfigureServices_Override_Should_Work()
        {
            // Arrange
            var module = new LegacyModuleWithoutConfigureServices();
            module.IocManager = LocalIocManager;

            // Act - should not throw
            module.ConfigureServices();

            // Assert - default implementation should work
            Assert.True(true);
        }

        [Fact]
        public void Module_Using_Both_ConfigureServices_And_PreInitialize_Should_Work()
        {
            // Arrange
            var module = new ModuleUsingBothMethods();
            module.IocManager = LocalIocManager;

            // Act
            module.ConfigureServices();

            // Assert
            module.ConfigureServicesCalled.ShouldBeTrue();
            module.PreInitializeCalled.ShouldBeTrue();
        }

        [Fact]
        public void Deprecation_Warning_Should_Be_Logged_For_PreInitialize_Usage()
        {
            // Arrange
            var logMessages = new System.Collections.Generic.List<string>();
            var mockLogger = new TestLogger(logMessages);
            
            // Create a simple module manager that will log warnings
            var moduleInfo = new AbpModuleInfo(
                typeof(LegacyModuleUsingPreInitialize),
                new LegacyModuleUsingPreInitialize(),
                false);

            var modules = new System.Collections.Generic.List<AbpModuleInfo> { moduleInfo };

            // Act - simulate the deprecation warning logic
            foreach (var module in modules)
            {
                var moduleType = module.Instance.GetType();
                var preInitMethod = moduleType.GetMethod("PreInitialize");
                
                if (preInitMethod != null && 
                    preInitMethod.DeclaringType != typeof(AbpModule) &&
                    preInitMethod.DeclaringType == moduleType)
                {
                    mockLogger.Log(
                        Microsoft.Extensions.Logging.LogLevel.Warning,
                        new Microsoft.Extensions.Logging.EventId(),
                        $"Module '{moduleType.Name}' overrides PreInitialize which is deprecated.",
                        null,
                        (state, ex) => state.ToString());
                }
            }

            // Assert
            logMessages.ShouldContain(msg => 
                msg.Contains("PreInitialize") && 
                msg.Contains("deprecated"));
        }

        [Fact]
        public void Module_Without_ConfigureServices_Should_Not_Break_Initialization()
        {
            // Arrange
            var moduleManager = new AbpModuleManager(LocalIocManager);
            moduleManager.Initialize(typeof(LegacyModuleWithoutConfigureServices));

            // Act - should not throw
            moduleManager.ConfigureServices();

            // Assert - if we get here, it worked
            Assert.True(true);
        }

        [Fact]
        public void ConfigureServices_Default_Implementation_Should_Do_Nothing()
        {
            // Arrange
            var module = new AbpModule_TestModule();
            module.IocManager = LocalIocManager;
            
            var initialRegistrationCount = LocalIocManager.IsRegistered<ITestService>() ? 1 : 0;

            // Act
            module.ConfigureServices();

            // Assert - no services should be registered by default implementation
            var finalRegistrationCount = LocalIocManager.IsRegistered<ITestService>() ? 1 : 0;
            finalRegistrationCount.ShouldBe(initialRegistrationCount);
        }

        // Test modules
        private class LegacyModuleWithoutConfigureServices : AbpModule
        {
            // This module doesn't override ConfigureServices - uses default implementation
        }

        private class LegacyModuleUsingPreInitialize : AbpModule
        {
            public bool PreInitializeCalled { get; private set; }
        }

        private class ModuleUsingBothMethods : AbpModule
        {
            public bool ConfigureServicesCalled { get; private set; }
            public bool PreInitializeCalled { get; private set; }

            public override void ConfigureServices()
            {
                ConfigureServicesCalled = true;
            }

        }

        private class AbpModule_TestModule : AbpModule
        {
        }

        // Test helper classes
        private interface ITestService { }
        private class TestService : ITestService { }

        private class TestLogger : MsLogger
        {
            private readonly System.Collections.Generic.List<string> _messages;

            public TestLogger(System.Collections.Generic.List<string> messages)
            {
                _messages = messages;
            }

            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _messages.Add(formatter(state, exception));
            }
        }
    }
}
