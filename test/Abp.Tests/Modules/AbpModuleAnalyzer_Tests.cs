using System;
using Abp.Modules;
using Shouldly;
using Xunit;

namespace Abp.Tests.Modules
{
    /// <summary>
    /// Tests for the AbpModuleAnalyzer utility.
    /// Requirements: 10.1, 10.2, 10.3
    /// </summary>
    public class AbpModuleAnalyzer_Tests
    {

        [Fact]
        public void Should_Not_Detect_PreInitialize_When_Not_Overridden()
        {
            // Act
            var result = AbpModuleAnalyzer.OverridesPreInitialize(typeof(ModuleWithoutPreInitialize));

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Should_Detect_ConfigureServices_Override()
        {
            // Act
            var result = AbpModuleAnalyzer.OverridesConfigureServices(typeof(ModuleWithConfigureServices));

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Detect_ConfigureServices_When_Not_Overridden()
        {
            // Act
            var result = AbpModuleAnalyzer.OverridesConfigureServices(typeof(ModuleWithoutConfigureServices));

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Should_Identify_Module_Following_Two_Phase_Lifecycle()
        {
            // Act
            var result = AbpModuleAnalyzer.FollowsTwoPhaseLifecycle(typeof(ModuleWithConfigureServices));

            // Assert
            result.ShouldBeTrue();
        }


        [Fact]
        public void Should_Return_Empty_Suggestion_For_Migrated_Module()
        {
            // Act
            var suggestion = AbpModuleAnalyzer.GetMigrationSuggestion(typeof(ModuleWithConfigureServices));

            // Assert
            suggestion.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Generate_Positive_Report_For_Migrated_Module()
        {
            // Act
            var report = AbpModuleAnalyzer.GetAnalysisReport(typeof(ModuleWithConfigureServices));

            // Assert
            report.ShouldNotBeEmpty();
            report.ShouldContain("Module Analysis");
            report.ShouldContain("ModuleWithConfigureServices");
            report.ShouldContain("best practices");
        }

        [Fact]
        public void Should_Throw_For_Non_Module_Type()
        {
            // Act & Assert
            Should.Throw<ArgumentException>(() => 
                AbpModuleAnalyzer.OverridesPreInitialize(typeof(string)));
        }

        [Fact]
        public void Should_Handle_Module_With_Both_Methods()
        {
            // Act
            var hasPreInit = AbpModuleAnalyzer.OverridesPreInitialize(typeof(ModuleWithBothMethods));
            var hasConfigureServices = AbpModuleAnalyzer.OverridesConfigureServices(typeof(ModuleWithBothMethods));
            var followsBestPractices = AbpModuleAnalyzer.FollowsTwoPhaseLifecycle(typeof(ModuleWithBothMethods));

            // Assert
            hasPreInit.ShouldBeTrue();
            hasConfigureServices.ShouldBeTrue();
            followsBestPractices.ShouldBeFalse(); // Still needs to remove PreInitialize
        }

        private class ModuleWithoutPreInitialize : AbpModule
        {
            // Uses default PreInitialize
        }

        private class ModuleWithConfigureServices : AbpModule
        {
            public override void ConfigureServices()
            {
                // Modern implementation
            }
        }

        private class ModuleWithoutConfigureServices : AbpModule
        {
            // Uses default ConfigureServices
        }

        private class ModuleWithBothMethods : AbpModule
        {
            public override void ConfigureServices()
            {
                // Modern implementation
            }
        }
    }
}
