using System;
using System.Reflection;

namespace Abp.Modules
{
    /// <summary>
    /// Utility class to analyze ABP modules for migration issues and compatibility concerns.
    /// </summary>
    public static class AbpModuleAnalyzer
    {
        /// <summary>
        /// Checks if a module overrides the deprecated PreInitialize method.
        /// </summary>
        /// <param name="moduleType">The module type to check</param>
        /// <returns>True if the module overrides PreInitialize, false otherwise</returns>
        public static bool OverridesPreInitialize(Type moduleType)
        {
            if (!typeof(AbpModule).IsAssignableFrom(moduleType))
            {
                throw new ArgumentException($"Type {moduleType.FullName} is not an ABP module.");
            }

            var preInitMethod = moduleType.GetMethod("PreInitialize");
            
            // Check if the module overrides PreInitialize (not just inheriting the base implementation)
            return preInitMethod != null && 
                   preInitMethod.DeclaringType != typeof(AbpModule) &&
                   preInitMethod.DeclaringType == moduleType;
        }

        /// <summary>
        /// Checks if a module overrides the ConfigureServices method.
        /// </summary>
        /// <param name="moduleType">The module type to check</param>
        /// <returns>True if the module overrides ConfigureServices, false otherwise</returns>
        public static bool OverridesConfigureServices(Type moduleType)
        {
            if (!typeof(AbpModule).IsAssignableFrom(moduleType))
            {
                throw new ArgumentException($"Type {moduleType.FullName} is not an ABP module.");
            }

            var configureServicesMethod = moduleType.GetMethod("ConfigureServices");
            
            // Check if the module overrides ConfigureServices (not just inheriting the base implementation)
            return configureServicesMethod != null && 
                   configureServicesMethod.DeclaringType != typeof(AbpModule) &&
                   configureServicesMethod.DeclaringType == moduleType;
        }

        /// <summary>
        /// Gets a migration suggestion message for a module that uses PreInitialize.
        /// </summary>
        /// <param name="moduleType">The module type</param>
        /// <returns>A helpful migration message</returns>
        public static string GetMigrationSuggestion(Type moduleType)
        {
            if (!OverridesPreInitialize(moduleType))
            {
                return string.Empty;
            }

            var hasConfigureServices = OverridesConfigureServices(moduleType);

            if (hasConfigureServices)
            {
                return $"Module '{moduleType.Name}' uses both ConfigureServices and PreInitialize. " +
                       "Consider removing PreInitialize if all service registrations have been moved to ConfigureServices.";
            }
            else
            {
                return $"Module '{moduleType.Name}' uses the deprecated PreInitialize method. " +
                       "Migration steps:\n" +
                       "1. Override the ConfigureServices() method in your module\n" +
                       "2. Move all IocManager.Register() calls from PreInitialize to ConfigureServices\n" +
                       "3. Keep configuration logic that uses resolved services in PreInitialize (temporarily) or move to Initialize\n" +
                       "4. Remove the PreInitialize override once migration is complete\n" +
                       "Example:\n" +
                       "  public override void ConfigureServices()\n" +
                       "  {\n" +
                       "      IocManager.Register<IMyService, MyServiceImpl>();\n" +
                       "  }";
            }
        }

        /// <summary>
        /// Validates that a module follows the two-phase lifecycle pattern.
        /// </summary>
        /// <param name="moduleType">The module type to validate</param>
        /// <returns>True if the module follows best practices, false otherwise</returns>
        public static bool FollowsTwoPhaseLifecycle(Type moduleType)
        {
            // A module follows the two-phase lifecycle if:
            // 1. It overrides ConfigureServices (for service registration)
            // 2. It does NOT override PreInitialize (deprecated)
            
            var hasConfigureServices = OverridesConfigureServices(moduleType);
            var hasPreInitialize = OverridesPreInitialize(moduleType);

            return hasConfigureServices && !hasPreInitialize;
        }

        /// <summary>
        /// Gets a detailed analysis report for a module.
        /// </summary>
        /// <param name="moduleType">The module type to analyze</param>
        /// <returns>A detailed analysis report</returns>
        public static string GetAnalysisReport(Type moduleType)
        {
            if (!typeof(AbpModule).IsAssignableFrom(moduleType))
            {
                return $"Type {moduleType.FullName} is not an ABP module.";
            }

            var report = $"Module Analysis: {moduleType.Name}\n";
            report += "=====================================\n";
            
            var hasConfigureServices = OverridesConfigureServices(moduleType);
            var hasPreInitialize = OverridesPreInitialize(moduleType);
            var followsBestPractices = FollowsTwoPhaseLifecycle(moduleType);

            report += $"Overrides ConfigureServices: {(hasConfigureServices ? "Yes" : "No")}\n";
            report += $"Overrides PreInitialize: {(hasPreInitialize ? "Yes (Deprecated)" : "No")}\n";
            report += $"Follows Two-Phase Lifecycle: {(followsBestPractices ? "Yes" : "No")}\n";

            if (!followsBestPractices)
            {
                report += "\nMigration Needed:\n";
                report += GetMigrationSuggestion(moduleType);
            }
            else
            {
                report += "\nStatus: Module follows best practices ✓";
            }

            return report;
        }
    }
}
