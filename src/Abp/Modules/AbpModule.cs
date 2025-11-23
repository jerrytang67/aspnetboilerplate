using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Abp.Collections.Extensions;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Logging;

namespace Abp.Modules {
    /// <summary>
    /// This class must be implemented by all module definition classes.
    /// </summary>
    /// <remarks>
    /// A module definition class is generally located in its own assembly
    /// and implements some action in module events on application startup and shutdown.
    /// It also defines depended modules.
    /// </remarks>
    public abstract class AbpModule {
        /// <summary>
        /// Gets a reference to the IOC manager.
        /// </summary>
        protected internal IIocManager IocManager { get; internal set; }

        /// <summary>
        /// Gets a reference to the ABP configuration.
        /// </summary>
        protected internal IAbpStartupConfiguration Configuration { get; internal set; }

        protected AbpModule() {
        }

        /// <summary>
        /// This method is called during the service configuration phase, before the dependency injection container is built.
        /// Use this method to register all services for this module with the IocManager.
        /// This is part of the two-phase lifecycle:
        /// Phase 1 (ConfigureServices): Register services before container construction
        /// Phase 2 (Initialize/PostInitialize): Use resolved services after container construction
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Do not resolve services in this method as the container is not yet built.
        /// Only register services using IocManager.Register methods.
        /// </remarks>
        public virtual void ConfigureServices() {
            // Default implementation does nothing
            // Derived modules should override this to register their services
        }

        /// <summary>
        /// This method is called after all modules have called ConfigureServices(), but before the container is built.
        /// Use this method for service replacements or other operations that need to happen after all services are registered.
        /// </summary>
        /// <remarks>
        /// This is useful for scenarios like:
        /// - Replacing services registered by other modules
        /// - Performing final configuration that depends on all modules being configured
        /// IMPORTANT: Do not resolve services in this method as the container is not yet built.
        /// </remarks>
        public virtual void PostConfigureServices() {
            // Default implementation does nothing
            // Derived modules should override this if needed
        }

        /// <summary>
        /// This method is called during the application initialization phase, after the dependency injection container is built.
        /// Use this method to initialize the module using resolved services from the IocManager.
        /// This is part of the two-phase lifecycle:
        /// Phase 1 (ConfigureServices): Register services before container construction
        /// Phase 2 (Initialize/PostInitialize): Use resolved services after container construction
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Do not register services in this method. The container is already built and immutable.
        /// Only resolve and use services that were registered in ConfigureServices.
        /// </remarks>
        public virtual void Initialize() {
        }

        /// <summary>
        /// This method is called when the application is being shutdown.
        /// </summary>
        public virtual void Shutdown() {
        }

        public virtual Assembly[] GetAdditionalAssemblies() {
            return new Assembly[0];
        }

        /// <summary>
        /// Checks if given type is an Abp module class.
        /// </summary>
        /// <param name="type">Type to check</param>
        public static bool IsAbpModule(Type type) {
            var typeInfo = type.GetTypeInfo();
            return
                typeInfo.IsClass &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                typeof(AbpModule).IsAssignableFrom(type);
        }

        /// <summary>
        /// Finds direct depended modules of a module (excluding given module).
        /// </summary>
        public static List<Type> FindDependedModuleTypes(Type moduleType) {
            if (!IsAbpModule(moduleType)) {
                throw new AbpInitializationException("This type is not an ABP module: " + moduleType.AssemblyQualifiedName);
            }

            var list = new List<Type>();

            if (moduleType.GetTypeInfo().IsDefined(typeof(DependsOnAttribute), true)) {
                var dependsOnAttributes = moduleType.GetTypeInfo().GetCustomAttributes(typeof(DependsOnAttribute), true).Cast<DependsOnAttribute>();
                foreach (var dependsOnAttribute in dependsOnAttributes) {
                    foreach (var dependedModuleType in dependsOnAttribute.DependedModuleTypes) {
                        list.Add(dependedModuleType);
                    }
                }
            }

            return list;
        }

        public static List<Type> FindDependedModuleTypesRecursivelyIncludingGivenModule(Type moduleType) {
            var list = new List<Type>();
            AddModuleAndDependenciesRecursively(list, moduleType);
            list.AddIfNotContains(typeof(AbpKernelModule));
            return list;
        }

        private static void AddModuleAndDependenciesRecursively(List<Type> modules, Type module) {
            if (!IsAbpModule(module)) {
                throw new AbpInitializationException("This type is not an ABP module: " + module.AssemblyQualifiedName);
            }

            if (modules.Contains(module)) {
                return;
            }

            modules.Add(module);

            var dependedModules = FindDependedModuleTypes(module);
            foreach (var dependedModule in dependedModules) {
                AddModuleAndDependenciesRecursively(modules, dependedModule);
            }
        }
    }
}