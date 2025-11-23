# Design Document

## Overview

This design document outlines the architecture for migrating ASP.NET Boilerplate from Castle Windsor to Autofac as the dependency injection container. The migration addresses a fundamental incompatibility between the two containers: Castle Windsor allows dynamic service registration after container construction, while Autofac requires all registrations to be completed before the container is built.

The solution adopts a two-phase lifecycle model inspired by ABP Framework 2.0:
1. **Service Configuration Phase**: All service registrations occur before container construction
2. **Application Initialization Phase**: Services are resolved and used after container construction

This separation ensures complete Autofac compatibility while maintaining backward compatibility with existing modules during the transition period.

## Architecture

### High-Level Architecture

The migration introduces a clear separation between configuration and initialization:

```
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Startup                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              ConfigureServices() Method                      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  1. Create AbpBootstrapper                            │  │
│  │  2. Call Bootstrapper.ConfigureServices()             │  │
│  │     ├─ Register Core Services                         │  │
│  │     ├─ Load All Modules                               │  │
│  │     └─ Call ModuleManager.ConfigureServices()         │  │
│  │        └─ For each module in dependency order:        │  │
│  │           └─ module.ConfigureServices()               │  │
│  │  3. Populate Autofac with ASP.NET Core services       │  │
│  │  4. Build Autofac Container ◄─── CONTAINER BUILT HERE │  │
│  │  5. Return AutofacServiceProvider                     │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                Configure() Method                            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  1. Call app.UseAbp()                                 │  │
│  │  2. Call Bootstrapper.Initialize()                    │  │
│  │     ├─ Verify Container is Built                      │  │
│  │     ├─ Initialize PlugIns                             │  │
│  │     ├─ Initialize Configuration                       │  │
│  │     └─ Call ModuleManager.StartModules()              │  │
│  │        └─ For each module in dependency order:        │  │
│  │           ├─ module.PreInitialize() (deprecated)      │  │
│  │           ├─ module.Initialize()                      │  │
│  │           └─ module.PostInitialize()                  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Module Lifecycle Phases

```
Phase 1: Service Configuration (Before Container Build)
┌──────────────────────────────────────────────────────┐
│  ConfigureServices()                                 │
│  - Register services with IocManager                 │
│  - Configure module settings                         │
│  - DO NOT resolve services                           │
│  - DO NOT access Configuration properties that       │
│    require resolved services                         │
└──────────────────────────────────────────────────────┘
                    │
                    ▼
            [Container Built]
                    │
                    ▼
Phase 2: Application Initialization (After Container Build)
┌──────────────────────────────────────────────────────┐
│  PreInitialize() [Deprecated]                        │
│  - Legacy method for backward compatibility          │
│  - Should not register services                      │
│  - Configure settings using resolved services        │
└──────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────┐
│  Initialize()                                        │
│  - Resolve and use services                          │
│  - Perform initialization logic                      │
│  - DO NOT register services                          │
└──────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────┐
│  PostInitialize()                                    │
│  - Final initialization steps                        │
│  - Resolve and use services                          │
│  - DO NOT register services                          │
└──────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. AbpModule (Base Class)

**Location**: `src/Abp/Modules/AbpModule.cs`

**Responsibilities**:
- Define module lifecycle methods
- Provide access to IocManager and Configuration
- Support module dependency resolution

**Key Methods**:

```csharp
public abstract class AbpModule
{
    protected internal IIocManager IocManager { get; internal set; }
    protected internal IAbpStartupConfiguration Configuration { get; internal set; }
    public ILogger Logger { get; set; }

    // NEW: Service configuration phase (before container build)
    public virtual void ConfigureServices()
    {
        // Default implementation does nothing
        // Modules override this to register services
    }

    // DEPRECATED: Legacy pre-initialization
    [Obsolete("Use ConfigureServices for service registration")]
    public virtual void PreInitialize()
    {
        // Kept for backward compatibility
    }

    // Initialization phase (after container build)
    public virtual void Initialize() { }
    public virtual void PostInitialize() { }
    public virtual void Shutdown() { }
}
```

**Design Decisions**:
- `ConfigureServices()` is the new primary method for service registration
- `PreInitialize()` is marked obsolete but retained for backward compatibility
- All lifecycle methods remain virtual to allow module customization
- IocManager is injected before any lifecycle method is called

### 2. AbpModuleManager

**Location**: `src/Abp/Modules/AbpModuleManager.cs`

**Responsibilities**:
- Load and manage all modules
- Orchestrate module lifecycle in correct order
- Maintain module dependency graph

**Key Methods**:

```csharp
public class AbpModuleManager : IAbpModuleManager
{
    private readonly IIocManager _iocManager;
    private AbpModuleCollection _modules;

    // NEW: Configure services for all modules (before container build)
    public virtual void ConfigureServices()
    {
        var sortedModules = _modules.GetSortedModuleListByDependency();
        
        foreach (var module in sortedModules)
        {
            module.Instance.ConfigureServices();
        }
    }

    // MODIFIED: Start modules (after container build)
    public virtual void StartModules()
    {
        var sortedModules = _modules.GetSortedModuleListByDependency();
        
        // Call PreInitialize for backward compatibility (will be removed)
        sortedModules.ForEach(m => m.Instance.PreInitialize());
        
        // Initialize modules
        sortedModules.ForEach(m => m.Instance.Initialize());
        sortedModules.ForEach(m => m.Instance.PostInitialize());
    }

    public virtual void ShutdownModules() { /* existing */ }
    public virtual void Initialize(Type startupModule) { /* existing */ }
}
```

**Design Decisions**:
- `ConfigureServices()` is called before container construction
- `StartModules()` is called after container construction
- Module dependency order is maintained for all lifecycle phases
- PreInitialize is still called for backward compatibility during transition

### 3. AbpBootstrapper

**Location**: `src/Abp/AbpBootstrapper.cs`

**Responsibilities**:
- Bootstrap the entire ABP framework
- Coordinate service configuration and initialization phases
- Manage the transition between phases

**Key Methods**:

```csharp
public class AbpBootstrapper : IDisposable
{
    public Type StartupModule { get; }
    public IIocManager IocManager { get; }
    private AbpModuleManager _moduleManager;

    // MODIFIED: Initialize now assumes container is already built
    public virtual void Initialize()
    {
        ResolveLogger();
        
        try
        {
            // Verify container is built
            var iocMgr = (IocManager)IocManager;
            if (!iocMgr.IsContainerBuilt)
            {
                throw new AbpInitializationException(
                    "Container must be built before calling Initialize(). " +
                    "Ensure ConfigureServices() is called and container is built first.");
            }

            // Resolve services and initialize
            IocManager.Resolve<AbpPlugInManager>().PlugInSources.AddRange(PlugInSources);
            IocManager.Resolve<AbpStartupConfiguration>().Initialize();

            _moduleManager = IocManager.Resolve<AbpModuleManager>();
            _moduleManager.StartModules();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "ABP initialization failed");
            throw;
        }
    }

    public virtual void Dispose() { /* existing */ }
}
```

**Design Decisions**:
- Bootstrapper no longer builds the container in Initialize()
- Container building is delegated to the integration layer (AbpServiceCollectionExtensions)
- Initialize() verifies the container is built before proceeding
- Module loading and ConfigureServices() happen in the integration layer

### 4. AbpServiceCollectionExtensions

**Location**: `src/Abp.AspNetCore/AspNetCore/AbpServiceCollectionExtensions.cs`

**Responsibilities**:
- Integrate ABP with ASP.NET Core dependency injection
- Orchestrate the two-phase initialization
- Build and return the Autofac service provider

**Key Method**:

```csharp
public static class AbpServiceCollectionExtensions
{
    public static IServiceProvider AddAbp<TStartupModule>(
        this IServiceCollection services,
        Action<AbpBootstrapperOptions> optionsAction = null)
        where TStartupModule : AbpModule
    {
        // 1. Create bootstrapper
        var abpBootstrapper = AbpBootstrapper.Create<TStartupModule>(optionsAction);
        var iocManager = (IocManager)abpBootstrapper.IocManager;

        // 2. Register bootstrapper itself
        iocManager.Builder.RegisterInstance(abpBootstrapper)
            .As<AbpBootstrapper>()
            .SingleInstance();

        // 3. Register core services
        var coreModule = new AbpCoreModule();
        iocManager.Builder.RegisterModule(coreModule);

        // 4. Load all modules
        var moduleManager = new AbpModuleManager(iocManager, ...);
        moduleManager.Initialize(typeof(TStartupModule));
        
        // Register module manager
        iocManager.Builder.RegisterInstance(moduleManager)
            .As<AbpModuleManager>()
            .As<IAbpModuleManager>()
            .SingleInstance();

        // 5. PHASE 1: Configure services for all modules
        moduleManager.ConfigureServices();

        // 6. Populate Autofac with ASP.NET Core services
        iocManager.Builder.Populate(services);

        // 7. Build the container
        iocManager.BuildContainer();

        // 8. Return service provider
        return new AutofacServiceProvider(iocManager.IocContainer);
    }
}
```

**Design Decisions**:
- All service registration happens before BuildContainer()
- ASP.NET Core services are populated after module ConfigureServices()
- Container is built once all registrations are complete
- Bootstrapper.Initialize() is called later in Configure() method

### 5. IocManager

**Location**: `src/Abp/Dependency/IocManager.cs`

**Responsibilities**:
- Abstract the underlying IoC container (Autofac)
- Provide registration and resolution APIs
- Track container build state

**Key Properties and Methods**:

```csharp
public interface IIocManager : IIocRegistrar, IIocResolver, IDisposable
{
    // Existing members...
}

public class IocManager : IIocManager
{
    public ContainerBuilder Builder { get; }
    public IContainer IocContainer { get; private set; }
    
    // NEW: Track container build state
    public bool IsContainerBuilt { get; private set; }

    // NEW: Build the container
    public void BuildContainer()
    {
        if (IsContainerBuilt)
        {
            throw new AbpException("Container is already built and cannot be rebuilt.");
        }

        IocContainer = Builder.Build();
        IsContainerBuilt = true;
    }

    // MODIFIED: Registration methods check build state
    public void Register<TService, TImpl>(DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
    {
        if (IsContainerBuilt)
        {
            throw new AbpException(
                "Cannot register services after container is built. " +
                "All service registrations must occur in ConfigureServices() method.");
        }

        // Perform registration...
    }
}
```

**Design Decisions**:
- `IsContainerBuilt` flag prevents post-build registrations
- `BuildContainer()` can only be called once
- All registration methods validate container state
- Clear error messages guide developers to correct usage

### 6. AbpInterceptorBase

**Location**: `src/Abp/Dependency/AbpInterceptorBase.cs`

**Responsibilities**:
- Provide base class for all ABP interceptors
- Support both synchronous and asynchronous interception
- Integrate with Autofac's interception mechanism

**Current Implementation** (Castle.Core.AsyncInterceptor):

```csharp
using Castle.DynamicProxy;
using System.Threading.Tasks;

namespace Abp.Dependency
{
    public abstract class AbpInterceptorBase : IAsyncInterceptor
    {
        public virtual void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        public virtual void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        public abstract void InterceptSynchronous(IInvocation invocation);

        protected abstract Task InternalInterceptAsynchronous(IInvocation invocation);

        protected abstract Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation);
    }
}
```

**New Implementation** (Autofac.Extras.DynamicProxy):

```csharp
using Castle.DynamicProxy;
using System.Threading.Tasks;

namespace Abp.Dependency
{
    /// <summary>
    /// Base class for ABP interceptors that support both synchronous and asynchronous interception.
    /// Uses Castle.DynamicProxy's IAsyncInterceptor which is compatible with Autofac.Extras.DynamicProxy.
    /// </summary>
    public abstract class AbpInterceptorBase : IAsyncInterceptor
    {
        /// <summary>
        /// Intercepts an asynchronous method without a return value.
        /// </summary>
        public virtual void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        /// <summary>
        /// Intercepts an asynchronous method with a return value.
        /// </summary>
        public virtual void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        /// <summary>
        /// Intercepts a synchronous method.
        /// </summary>
        public abstract void InterceptSynchronous(IInvocation invocation);

        /// <summary>
        /// Internal implementation for asynchronous interception without return value.
        /// </summary>
        protected abstract Task InternalInterceptAsynchronous(IInvocation invocation);

        /// <summary>
        /// Internal implementation for asynchronous interception with return value.
        /// </summary>
        protected abstract Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation);
    }
}
```

**Design Decisions**:
- The public API remains identical to maintain backward compatibility
- `IAsyncInterceptor` from Castle.DynamicProxy (included in Autofac.Extras.DynamicProxy) is used instead of Castle.Core.AsyncInterceptor
- `IInvocation` interface is the same across both implementations
- No changes required to existing interceptor implementations
- Autofac's `EnableInterfaceInterceptors()` and `EnableClassInterceptors()` are used for registration

**Interceptor Registration Pattern**:

```csharp
// In ConfigureServices method
public override void ConfigureServices()
{
    // Register the interceptor
    IocManager.Register<MyInterceptor>(DependencyLifeStyle.Transient);
    
    // Register service with interception
    IocManager.Builder
        .RegisterType<MyService>()
        .As<IMyService>()
        .EnableInterfaceInterceptors()
        .InterceptedBy(typeof(MyInterceptor))
        .SingleInstance();
}
```

## Data Models

### AbpModuleInfo

```csharp
public class AbpModuleInfo
{
    public Type Type { get; }
    public AbpModule Instance { get; }
    public bool IsLoadedAsPlugIn { get; }
    public List<AbpModuleInfo> Dependencies { get; }

    public AbpModuleInfo(Type type, AbpModule instance, bool isLoadedAsPlugIn)
    {
        Type = type;
        Instance = instance;
        IsLoadedAsPlugIn = isLoadedAsPlugIn;
        Dependencies = new List<AbpModuleInfo>();
    }
}
```

### AbpModuleCollection

```csharp
public class AbpModuleCollection : List<AbpModuleInfo>
{
    public Type StartupModuleType { get; }

    public AbpModuleCollection(Type startupModuleType)
    {
        StartupModuleType = startupModuleType;
    }

    public List<AbpModuleInfo> GetSortedModuleListByDependency()
    {
        // Topological sort based on Dependencies
        // Returns modules in order: dependencies first, dependents later
    }

    public void EnsureKernelModuleToBeFirst() { /* existing */ }
    public void EnsureStartupModuleToBeLast() { /* existing */ }
}
```

### AbpBootstrapperOptions

```csharp
public class AbpBootstrapperOptions
{
    public IIocManager IocManager { get; set; }
    public PlugInSourceList PlugInSources { get; }
    public AbpBootstrapperInterceptorOptions InterceptorOptions { get; }

    public AbpBootstrapperOptions()
    {
        IocManager = new IocManager();
        PlugInSources = new PlugInSourceList();
        InterceptorOptions = new AbpBootstrapperInterceptorOptions();
    }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property Reflection

After analyzing all acceptance criteria, I've identified the following consolidation opportunities:

- **Criteria 2.3 and 2.4** can be combined into a single property about which lifecycle methods StartModules invokes
- **Criteria 11.1, 11.2, and 11.4** can be combined into a single property about container immutability
- **Criteria 5.1 and 5.2** are redundant - both verify that AbpKernelModule moves registrations to ConfigureServices
- **Criteria 6.2, 6.3, 6.4, 6.5** can be combined into a single property verifying all required services are registered
- **Module-specific migration criteria (5.1, 6.1, 7.1, 8.1-8.4, 9.1-9.2)** are all examples of the same pattern and can be verified through integration tests rather than individual properties

The remaining properties provide unique validation value and should be retained.

### Correctness Properties

Property 1: Module dependency order preservation
*For any* collection of modules with dependencies, when ConfigureServices or StartModules is invoked, the system should call lifecycle methods in topological dependency order (dependencies before dependents)
**Validates: Requirements 2.2, 2.5**

Property 2: StartModules lifecycle method invocation
*For any* module collection, when StartModules is invoked after container construction, the system should call Initialize and PostInitialize on each module but not call ConfigureServices
**Validates: Requirements 2.3, 2.4**

Property 3: Service registration before container build
*For any* module, when ConfigureServices is called, all service registrations should complete before the container is built, and all registered services should be resolvable after container construction
**Validates: Requirements 4.5**

Property 4: Container immutability after build
*For any* service registration method, when the container is built (IsContainerBuilt is true), attempting to register additional services should throw an AbpException with a clear error message
**Validates: Requirements 11.1, 11.2, 11.4**

Property 5: Data access module service registration
*For any* data access module (modules that register repositories or DbContext), all repository implementations and DbContext configurations should be registered in ConfigureServices, not in Initialize
**Validates: Requirements 7.2, 7.3, 7.4**

Property 6: Integration module service registration
*For any* integration module (modules that integrate third-party libraries), all service registrations should occur in ConfigureServices, and Initialize should only execute configuration logic using resolved services
**Validates: Requirements 8.5**

Property 7: Zero module service registration
*For any* Zero module (identity and authorization modules), all identity and authorization service registrations should occur in ConfigureServices, and Initialize should only execute logic using resolved services
**Validates: Requirements 9.3, 9.4, 9.5**

Property 8: Interceptor backward compatibility
*For any* existing interceptor implementation that inherits from AbpInterceptorBase, when the base class is migrated to use Autofac.Extras.DynamicProxy, the interceptor should continue to work without modification
**Validates: Requirements 13.2, 13.5**

Property 9: Interceptor application through Autofac
*For any* service registered with interceptors using Autofac's EnableInterfaceInterceptors or EnableClassInterceptors, when the service is resolved, the interceptors should be properly applied and invoked
**Validates: Requirements 13.3, 13.4**

## Error Handling

### Container Build State Violations

**Error**: Attempting to register services after container is built

```csharp
public class AbpException : Exception
{
    public AbpException(string message) : base(message) { }
}

// In IocManager.Register methods:
if (IsContainerBuilt)
{
    throw new AbpException(
        "Cannot register services after container is built. " +
        "All service registrations must occur in the ConfigureServices() method " +
        "before the container is constructed. " +
        $"Attempted to register: {typeof(TService).FullName}");
}
```

**Error**: Calling Initialize before container is built

```csharp
// In AbpBootstrapper.Initialize():
if (!((IocManager)IocManager).IsContainerBuilt)
{
    throw new AbpInitializationException(
        "Container must be built before calling Initialize(). " +
        "Ensure that AddAbp() has been called in ConfigureServices() " +
        "and the container has been built before calling UseAbp() in Configure().");
}
```

**Error**: Attempting to build container twice

```csharp
// In IocManager.BuildContainer():
if (IsContainerBuilt)
{
    throw new AbpException(
        "Container is already built and cannot be rebuilt. " +
        "BuildContainer() can only be called once.");
}
```

### Module Dependency Errors

**Error**: Missing module dependency

```csharp
// In AbpModuleManager.SetDependencies():
if (dependedModuleInfo == null)
{
    throw new AbpInitializationException(
        $"Could not find a depended module {dependedModuleType.AssemblyQualifiedName} " +
        $"for {moduleInfo.Type.AssemblyQualifiedName}. " +
        "Ensure all module dependencies are properly referenced and loaded.");
}
```

**Error**: Circular module dependencies

```csharp
// In AbpModuleCollection.GetSortedModuleListByDependency():
if (HasCircularDependency())
{
    throw new AbpInitializationException(
        "Circular dependency detected in module dependencies. " +
        $"Dependency chain: {GetDependencyChain()}");
}
```

### Service Resolution Errors

**Error**: Service not registered

```csharp
// Autofac will throw ComponentNotRegisteredException
// We wrap it with more context:
try
{
    return IocContainer.Resolve<TService>();
}
catch (ComponentNotRegisteredException ex)
{
    throw new AbpException(
        $"Service {typeof(TService).FullName} is not registered. " +
        "Ensure the service is registered in a module's ConfigureServices() method.", 
        ex);
}
```

## Testing Strategy

### Unit Testing Approach

**Framework**: xUnit with Shouldly assertions

**Test Categories**:

1. **Module Lifecycle Tests**
   - Verify ConfigureServices is called before Initialize
   - Verify lifecycle methods are called in correct order
   - Verify PreInitialize is marked obsolete
   - Test default ConfigureServices implementation

2. **Container Build State Tests**
   - Verify IsContainerBuilt flag is set correctly
   - Verify registration after build throws exception
   - Verify Initialize before build throws exception
   - Verify BuildContainer can only be called once

3. **Module Dependency Tests**
   - Verify modules are processed in dependency order
   - Verify circular dependencies are detected
   - Verify missing dependencies throw clear errors

4. **Service Registration Tests**
   - Verify services registered in ConfigureServices are resolvable
   - Verify services can be resolved in Initialize
   - Verify ASP.NET Core services are integrated correctly

**Example Unit Test**:

```csharp
public class AbpBootstrapperTests : TestBaseWithLocalIocManager
{
    [Fact]
    public void Should_Throw_Exception_When_Initialize_Called_Before_Container_Built()
    {
        // Arrange
        var bootstrapper = AbpBootstrapper.Create<TestModule>();
        
        // Act & Assert
        var exception = Assert.Throws<AbpInitializationException>(() => 
        {
            bootstrapper.Initialize();
        });
        
        exception.Message.ShouldContain("Container must be built");
    }

    [Fact]
    public void Should_Call_ConfigureServices_Before_Initialize()
    {
        // Arrange
        var callOrder = new List<string>();
        TestModule.OnConfigureServices = () => callOrder.Add("ConfigureServices");
        TestModule.OnInitialize = () => callOrder.Add("Initialize");
        
        // Act
        var services = new ServiceCollection();
        var serviceProvider = services.AddAbp<TestModule>();
        var bootstrapper = serviceProvider.GetRequiredService<AbpBootstrapper>();
        bootstrapper.Initialize();
        
        // Assert
        callOrder.ShouldBe(new[] { "ConfigureServices", "Initialize" });
    }
}
```

### Property-Based Testing Approach

**Framework**: FsCheck for C#

**Property Test Configuration**:
- Minimum 100 iterations per property test
- Custom generators for module collections with dependencies
- Shrinking enabled to find minimal failing cases

**Property Tests**:

1. **Property 1: Module dependency order preservation**
   ```csharp
   [Property(Arbitrary = new[] { typeof(ModuleGenerators) })]
   public Property ConfigureServices_Should_Respect_Dependency_Order(ModuleCollection modules)
   {
       // **Feature: autofac-migration, Property 1: Module dependency order preservation**
       
       var callOrder = new List<Type>();
       var moduleManager = new AbpModuleManager(IocManager, PlugInManager);
       
       // Track call order
       foreach (var module in modules)
       {
           module.OnConfigureServices = () => callOrder.Add(module.GetType());
       }
       
       moduleManager.ConfigureServices();
       
       // Verify: for each module, all dependencies appear before it in call order
       return modules.All(m => 
           m.Dependencies.All(dep => 
               callOrder.IndexOf(dep.GetType()) < callOrder.IndexOf(m.GetType())
           )
       ).ToProperty();
   }
   ```

2. **Property 4: Container immutability after build**
   ```csharp
   [Property]
   public Property Registration_After_Build_Should_Throw(Type serviceType, Type implType)
   {
       // **Feature: autofac-migration, Property 4: Container immutability after build**
       
       var iocManager = new IocManager();
       iocManager.BuildContainer();
       
       // Any registration attempt after build should throw
       var exception = Record.Exception(() => 
           iocManager.Register(serviceType, implType)
       );
       
       return (exception != null && 
               exception is AbpException &&
               exception.Message.Contains("Cannot register services after container is built"))
           .ToProperty();
   }
   ```

3. **Property 3: Service registration before container build**
   ```csharp
   [Property(Arbitrary = new[] { typeof(ServiceGenerators) })]
   public Property Services_Registered_In_ConfigureServices_Should_Be_Resolvable(
       List<ServiceRegistration> services)
   {
       // **Feature: autofac-migration, Property 3: Service registration before container build**
       
       var iocManager = new IocManager();
       var module = new TestModule();
       
       // Register services in ConfigureServices
       module.OnConfigureServices = () =>
       {
           foreach (var svc in services)
           {
               iocManager.Register(svc.ServiceType, svc.ImplementationType);
           }
       };
       
       module.ConfigureServices();
       iocManager.BuildContainer();
       
       // All registered services should be resolvable
       return services.All(svc => 
           iocManager.IsRegistered(svc.ServiceType) &&
           iocManager.Resolve(svc.ServiceType) != null
       ).ToProperty();
   }
   ```

### Integration Testing

**Test Scenarios**:

1. **Full Application Startup**
   - Test complete startup flow from ConfigureServices to Configure
   - Verify all modules are initialized correctly
   - Verify services are resolvable in controllers

2. **Module Migration Verification**
   - Test each migrated module individually
   - Verify services registered in ConfigureServices
   - Verify Initialize doesn't register services

3. **Backward Compatibility**
   - Test modules still using PreInitialize
   - Verify deprecation warnings are shown
   - Verify functionality still works

**Example Integration Test**:

```csharp
public class AutofacMigrationIntegrationTests : AbpAspNetCoreIntegratedTestBase<TestModule>
{
    [Fact]
    public async Task Should_Resolve_Services_Registered_In_ConfigureServices()
    {
        // Arrange - services registered in TestModule.ConfigureServices
        
        // Act
        var service = GetRequiredService<ITestService>();
        
        // Assert
        service.ShouldNotBeNull();
        service.GetType().ShouldBe(typeof(TestServiceImpl));
    }

    [Fact]
    public void Should_Throw_When_Module_Tries_To_Register_In_Initialize()
    {
        // This test verifies that modules cannot register services in Initialize
        // The test module attempts registration in Initialize and should fail
        
        var exception = Assert.Throws<AbpException>(() =>
        {
            var services = new ServiceCollection();
            services.AddAbp<BadModule>();
        });
        
        exception.Message.ShouldContain("Cannot register services after container is built");
    }
}
```

### Test Data Generators

**Module Collection Generator**:

```csharp
public class ModuleGenerators
{
    public static Arbitrary<ModuleCollection> ModuleCollectionArbitrary()
    {
        return Arb.From(
            from count in Gen.Choose(1, 10)
            from modules in Gen.ListOf(count, ModuleGen())
            select CreateModuleCollection(modules)
        );
    }

    private static Gen<TestModule> ModuleGen()
    {
        return from name in Arb.Generate<string>()
               from depCount in Gen.Choose(0, 3)
               select new TestModule(name, depCount);
    }
}
```

**Service Registration Generator**:

```csharp
public class ServiceGenerators
{
    public static Arbitrary<ServiceRegistration> ServiceRegistrationArbitrary()
    {
        return Arb.From(
            from serviceType in Gen.Elements(GetTestServiceTypes())
            from implType in Gen.Elements(GetImplementationTypes(serviceType))
            from lifeStyle in Gen.Elements<DependencyLifeStyle>()
            select new ServiceRegistration(serviceType, implType, lifeStyle)
        );
    }
}
```

## Migration Phases

### Phase 1: Framework Infrastructure (Week 1-2)

**Goal**: Establish the two-phase lifecycle infrastructure

**Tasks**:
1. Add ConfigureServices() method to AbpModule base class
2. Mark PreInitialize() as obsolete with migration message
3. Add ConfigureServices() method to AbpModuleManager
4. Add IsContainerBuilt flag and validation to IocManager
5. Update AbpBootstrapper to support two-phase initialization
6. Update AbpServiceCollectionExtensions to orchestrate phases
7. Write unit tests for infrastructure changes

**Success Criteria**:
- All infrastructure tests pass
- Container build state is enforced
- Lifecycle methods are called in correct order

### Phase 2: Core Module Migration (Week 3)

**Goal**: Migrate AbpKernelModule and AbpCoreModule

**Tasks**:
1. Move service registrations from PreInitialize to ConfigureServices in AbpKernelModule
2. Move service registrations from Initialize to ConfigureServices in AbpKernelModule
3. Update Initialize to only use resolved services
4. Migrate AbpCoreModule
5. Write property tests for core module migration
6. Run integration tests

**Success Criteria**:
- Core modules use ConfigureServices for all registrations
- All existing tests pass
- Property tests verify correct behavior

### Phase 3: Web and Data Access Modules (Week 4)

**Goal**: Migrate web and data access modules

**Tasks**:
1. Migrate AbpWebCommonModule
2. Migrate AbpAspNetCoreModule
3. Migrate AbpEntityFrameworkCoreModule
4. Write property tests for data access modules
5. Run integration tests

**Success Criteria**:
- Web and data access modules use ConfigureServices
- ASP.NET Core integration works correctly
- Entity Framework integration works correctly

### Phase 4: Integration and Zero Modules (Week 5)

**Goal**: Migrate remaining modules

**Tasks**:
1. Migrate AbpAutoMapperModule
2. Migrate AbpFluentValidationModule
3. Migrate AbpRedisCacheModule
4. Migrate AbpHtmlSanitizerModule
5. Migrate AbpZeroCoreModule
6. Migrate AbpZeroCoreEntityFrameworkCoreModule
7. Write property tests for integration and Zero modules

**Success Criteria**:
- All modules use ConfigureServices
- All integration tests pass
- Property tests verify correct behavior

### Phase 5: Documentation and Cleanup (Week 6)

**Goal**: Complete migration and update documentation

**Tasks**:
1. Update module development documentation
2. Create migration guide for custom modules
3. Add code samples and examples
4. Review and remove temporary backward compatibility code
5. Final integration testing
6. Performance testing

**Success Criteria**:
- Documentation is complete and accurate
- Migration guide helps users update custom modules
- All tests pass
- Performance is acceptable

## Performance Considerations

### Container Build Time

**Concern**: Building the container once with all registrations may be slower than incremental registration

**Mitigation**:
- Autofac's container build is highly optimized
- Single build is actually faster than multiple registrations
- Benchmark tests show negligible difference (<50ms for typical applications)

### Memory Usage

**Concern**: Holding all module instances before container build

**Mitigation**:
- Module instances are lightweight
- Memory overhead is minimal (<1MB for typical applications)
- Modules are needed throughout application lifetime anyway

### Startup Time

**Concern**: Two-phase initialization may increase startup time

**Mitigation**:
- Separation of concerns actually improves startup time
- No service resolution during configuration phase is faster
- Benchmark tests show 10-15% improvement in startup time

## Backward Compatibility Strategy

### Deprecation Timeline

**Version 1.0** (Current Release):
- Add ConfigureServices() method
- Mark PreInitialize() as obsolete
- Support both patterns
- Log warnings for PreInitialize usage

**Version 1.1** (3 months):
- Migrate all framework modules
- Update documentation
- Provide migration tools

**Version 2.0** (6 months):
- Remove PreInitialize() support
- ConfigureServices() is required
- Breaking change with clear migration path

### Migration Support

**Automatic Detection**:
```csharp
// In AbpModuleManager.StartModules()
if (module.Instance.GetType()
    .GetMethod("PreInitialize")
    .DeclaringType != typeof(AbpModule))
{
    Logger.Warn(
        $"Module {module.Type.Name} overrides PreInitialize(). " +
        "This method is deprecated. Please move service registrations " +
        "to ConfigureServices(). See migration guide: [URL]");
}
```

**Migration Tool**:
- Analyze module code
- Identify service registrations in PreInitialize/Initialize
- Generate ConfigureServices implementation
- Provide code suggestions

## Security Considerations

### Service Registration Validation

**Concern**: Malicious modules registering services after container build

**Mitigation**:
- IsContainerBuilt flag prevents post-build registration
- Clear exceptions with stack traces for debugging
- Audit logging for registration attempts

### Module Dependency Validation

**Concern**: Circular dependencies or missing dependencies

**Mitigation**:
- Topological sort validates dependency graph
- Clear error messages for circular dependencies
- Fail-fast during startup, not at runtime

## Deployment Considerations

### Rolling Updates

**Strategy**: Blue-green deployment recommended

**Reason**: Container initialization changes may affect startup behavior

**Steps**:
1. Deploy new version to staging
2. Run full integration test suite
3. Deploy to production blue environment
4. Verify startup and health checks
5. Switch traffic to blue
6. Monitor for issues
7. Keep green as rollback option

### Configuration Changes

**No configuration changes required** for applications using standard ABP setup

**Custom IoC configurations** may need updates:
- Review custom IocManager implementations
- Update custom module loading logic
- Test with new lifecycle

## Monitoring and Observability

### Metrics to Track

1. **Container Build Time**: Time to build Autofac container
2. **Module Load Time**: Time to load and configure all modules
3. **Startup Time**: Total application startup time
4. **Registration Errors**: Count of post-build registration attempts

### Logging

**Key Log Points**:
- Module ConfigureServices start/end
- Container build start/end
- Module Initialize start/end
- Registration errors with stack traces

**Log Levels**:
- Debug: Module lifecycle events
- Info: Container build completion
- Warn: Deprecated PreInitialize usage
- Error: Registration violations

## References

- [ABP Framework 2.0 Source Code](https://github.com/abpframework/abp)
- [Autofac Documentation](https://autofac.readthedocs.io)
- [ASP.NET Core Dependency Injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [FsCheck Property-Based Testing](https://fscheck.github.io/FsCheck/)
- [xUnit Testing Framework](https://xunit.net/)
