using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Abp.Collections.Extensions;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Logging;

namespace Abp.Modules {
    /// <summary>
    /// This class is used to manage modules.
    /// </summary>
    public class AbpModuleManager : IAbpModuleManager {
        public AbpModuleInfo StartupModule { get; private set; }

        public IReadOnlyList<AbpModuleInfo> Modules => _modules.ToImmutableList();

        public ILogger Logger { get; set; }

        private AbpModuleCollection _modules;

        private readonly IIocManager _iocManager;

        /// <summary>
        /// Constructor for use before container is built.
        /// Creates a temporary PlugInManager that will be replaced after container build.
        /// </summary>
        public AbpModuleManager(IIocManager iocManager) {
            _iocManager = iocManager;
            Logger = iocManager.Resolve<ILoggerFactory>().CreateLogger<AbpModuleManager>();
        }

        public virtual void Initialize(Type startupModule) {
            _modules = new AbpModuleCollection(startupModule);
            LoadAllModules();
        }

        /// <summary>
        /// Configures services for all modules before the container is built.
        /// This method should be called before building the IoC container.
        /// </summary>
        public virtual void ConfigureServices() {
            var sortedModules = _modules.GetSortedModuleListByDependency();

            // Ensure all modules have their Configuration set before calling ConfigureServices
            // We need to create and early-initialize the configuration before the container is built
            IAbpStartupConfiguration configuration = null;
            try {
                // Create AbpStartupConfiguration instance
                var configurationType = Type.GetType("Abp.Configuration.Startup.AbpStartupConfiguration, Abp");
                if (configurationType != null) {
                    configuration = (IAbpStartupConfiguration)Activator.CreateInstance(configurationType, _iocManager);

                    // Call EarlyInitialize to set up configuration objects without resolving from container
                    var earlyInitMethod = configurationType.GetMethod("EarlyInitialize");
                    if (earlyInitMethod != null) {
                        earlyInitMethod.Invoke(configuration, null);
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogWarning("Failed to create and initialize IAbpStartupConfiguration instance: " + ex.Message);
                // If we can't create it, modules will need to handle null Configuration
                // This shouldn't happen in normal scenarios
            }

            // Set Configuration on all modules
            if (configuration != null) {
                foreach (var module in sortedModules) {
                    if (module.Instance.Configuration == null) {
                        module.Instance.Configuration = configuration;
                    }
                }
            }

            // Call ConfigureServices for all modules in dependency order
            foreach (var module in sortedModules) {
                Logger.LogDebug($"Calling ConfigureServices for module: {module.Type.Name}");
                Console.WriteLine($">>> ConfigureServices: {module.Type.Name}");

                // Check if container is already built before calling ConfigureServices
                var iocMgr = (IocManager)_iocManager;
                if (iocMgr.IsContainerBuilt) {
                    throw new AbpException(
                        $"Container is already built before calling ConfigureServices for module {module.Type.Name}! " +
                        "This should not happen. A previous module must have triggered container building.");
                }

                module.Instance.ConfigureServices();

                // Check if this module triggered container building
                if (iocMgr.IsContainerBuilt) {
                    var stackTrace = new System.Diagnostics.StackTrace(true);
                    throw new AbpException(
                        $"Module {module.Type.Name}.ConfigureServices() triggered container building! " +
                        "ConfigureServices should only register services, not resolve them.\n\n" +
                        "This usually happens when:\n" +
                        "1. Calling IocManager.Resolve() in ConfigureServices\n" +
                        "2. Accessing properties that lazy-resolve services\n" +
                        "3. Calling Configuration.Get<T>() that resolves services\n\n" +
                        "Check the debug output above to see the exact call stack.");
                }

                Console.WriteLine($"<<< ConfigureServices completed: {module.Type.Name}");
            }

            // Call PostConfigureServices for all modules (for service replacements, etc.)
            foreach (var module in sortedModules) {
                Logger.LogDebug($"Calling PostConfigureServices for module: {module.Type.Name}");
                module.Instance.PostConfigureServices();
            }

            Logger.LogDebug("All modules configured their services.");
        }

        /// <summary>
        /// Starts all modules after the container is built.
        /// This method should be called after the IoC container is built.
        /// </summary>
        public virtual void StartModules() {
            var sortedModules = _modules.GetSortedModuleListByDependency();

            // Ensure all modules have their Configuration set
            // (it might not have been set during CreateModules if container wasn't built yet)
            foreach (var module in sortedModules) {
                if (module.Instance.Configuration == null) {
                    module.Instance.Configuration = _iocManager.Resolve<IAbpStartupConfiguration>();
                }
            }

            // Check for PreInitialize usage and log deprecation warnings
            LogPreInitializeDeprecationWarnings(sortedModules);

            sortedModules.ForEach(module => module.Instance.Initialize());
        }

        /// <summary>
        /// Logs deprecation warnings for modules that override PreInitialize.
        /// </summary>
        private void LogPreInitializeDeprecationWarnings(List<AbpModuleInfo> modules) {
            foreach (var module in modules) {
                var moduleType = module.Instance.GetType();
                var preInitMethod = moduleType.GetMethod("PreInitialize");

                // Check if the module overrides PreInitialize (not just inheriting the base implementation)
                if (preInitMethod != null &&
                    preInitMethod.DeclaringType != typeof(AbpModule) &&
                    preInitMethod.DeclaringType == moduleType) {
                    Logger.LogWarning(
                        $"Module '{moduleType.Name}' overrides PreInitialize which is deprecated. " +
                        "Please migrate service registrations to ConfigureServices() method. " +
                        "PreInitialize will be removed in a future version.");
                }
            }
        }

        public virtual void ShutdownModules() {
            Logger.LogDebug("Shutting down has been started");

            var sortedModules = _modules.GetSortedModuleListByDependency();
            sortedModules.Reverse();
            sortedModules.ForEach(sm => sm.Instance.Shutdown());

            Logger.LogDebug("Shutting down completed.");
        }

        private void LoadAllModules() {
            Logger.LogDebug("Loading Abp modules...");

            List<Type> plugInModuleTypes;
            var moduleTypes = FindAllModuleTypes(out plugInModuleTypes).Distinct().ToList();

            Logger.LogDebug("Found " + moduleTypes.Count + " ABP modules in total.");

            RegisterModules(moduleTypes);
            CreateModules(moduleTypes, plugInModuleTypes);

            _modules.EnsureKernelModuleToBeFirst();
            _modules.EnsureStartupModuleToBeLast();

            SetDependencies();

            Logger.LogDebug("{0} modules loaded.", _modules.Count);
        }

        private List<Type> FindAllModuleTypes(out List<Type> plugInModuleTypes) {
            plugInModuleTypes = new List<Type>();

            var modules = AbpModule.FindDependedModuleTypesRecursivelyIncludingGivenModule(_modules.StartupModuleType);

            return modules;
        }

        private void CreateModules(ICollection<Type> moduleTypes, List<Type> plugInModuleTypes) {
            foreach (var moduleType in moduleTypes) {
                // Try to create module instance with dependency injection
                // First try to resolve dependencies from already registered services
                AbpModule moduleObject;

                try {
                    // Try to find a constructor and resolve its dependencies
                    var constructors = moduleType.GetConstructors();
                    if (constructors.Length == 0) {
                        throw new AbpInitializationException($"No public constructor found for module: {moduleType.AssemblyQualifiedName}");
                    }

                    // Try parameterless constructor first
                    var parameterlessConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                    if (parameterlessConstructor != null) {
                        moduleObject = (AbpModule)parameterlessConstructor.Invoke(null);
                    }
                    else {
                        // Try to resolve constructor parameters
                        var constructor = constructors.OrderBy(c => c.GetParameters().Length).First();
                        var parameters = constructor.GetParameters();
                        var parameterValues = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++) {
                            var paramType = parameters[i].ParameterType;
                            if (_iocManager.IsRegistered(paramType)) {
                                // Try to resolve if container is built, otherwise register and resolve
                                if (((IocManager)_iocManager).IsContainerBuilt) {
                                    parameterValues[i] = _iocManager.Resolve(paramType);
                                }
                                else {
                                    // For services needed before container build, try to create them
                                    // This is a temporary solution - ideally all modules should have parameterless constructors
                                    parameterValues[i] = Activator.CreateInstance(paramType);
                                }
                            }
                            else {
                                // Register the type if not registered
                                _iocManager.Register(paramType);
                                parameterValues[i] = Activator.CreateInstance(paramType);
                            }
                        }

                        moduleObject = (AbpModule)constructor.Invoke(parameterValues);
                    }
                }
                catch (Exception ex) {
                    throw new AbpInitializationException($"Failed to create instance of module: {moduleType.AssemblyQualifiedName}", ex);
                }

                if (moduleObject == null) {
                    throw new AbpInitializationException("This type is not an ABP module: " + moduleType.AssemblyQualifiedName);
                }

                moduleObject.IocManager = _iocManager;

                // We can't resolve IAbpStartupConfiguration yet because the container isn't built
                // It will be set later when the container is built and StartModules is called
                // For now, we'll resolve it lazily when needed
                try {
                    if (_iocManager.IsRegistered<IAbpStartupConfiguration>() && ((IocManager)_iocManager).IsContainerBuilt) {
                        moduleObject.Configuration = _iocManager.Resolve<IAbpStartupConfiguration>();
                    }
                }
                catch {
                    // Configuration will be set later in StartModules
                }

                var moduleInfo = new AbpModuleInfo(moduleType, moduleObject, plugInModuleTypes.Contains(moduleType));

                _modules.Add(moduleInfo);

                if (moduleType == _modules.StartupModuleType) {
                    StartupModule = moduleInfo;
                }

                Logger.LogDebug("Loaded module: " + moduleType.AssemblyQualifiedName);
            }
        }

        private void RegisterModules(ICollection<Type> moduleTypes) {
            foreach (var moduleType in moduleTypes) {
                _iocManager.RegisterIfNot(moduleType);
            }
        }

        private void SetDependencies() {
            foreach (var moduleInfo in _modules) {
                moduleInfo.Dependencies.Clear();

                //Set dependencies for defined DependsOnAttribute attribute(s).
                foreach (var dependedModuleType in AbpModule.FindDependedModuleTypes(moduleInfo.Type)) {
                    var dependedModuleInfo = _modules.FirstOrDefault(m => m.Type == dependedModuleType);
                    if (dependedModuleInfo == null) {
                        throw new AbpInitializationException("Could not find a depended module " + dependedModuleType.AssemblyQualifiedName + " for " +
                                                             moduleInfo.Type.AssemblyQualifiedName);
                    }

                    if ((moduleInfo.Dependencies.FirstOrDefault(dm => dm.Type == dependedModuleType) == null)) {
                        moduleInfo.Dependencies.Add(dependedModuleInfo);
                    }
                }
            }
        }
    }
}