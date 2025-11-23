# Troubleshooting Guide

This guide helps you diagnose and fix common issues when migrating to the Autofac two-phase lifecycle.

## Table of Contents

1. [Container Build Errors](#container-build-errors)
2. [Service Registration Errors](#service-registration-errors)
3. [Service Resolution Errors](#service-resolution-errors)
4. [Module Dependency Errors](#module-dependency-errors)
5. [Initialization Errors](#initialization-errors)
6. [Runtime Errors](#runtime-errors)
7. [Performance Issues](#performance-issues)

## Container Build Errors

### Error: "Cannot register services after container is built"

**Full Error Message:**
```
Abp.AbpException: Cannot register services after container is built.
All service registrations must occur in the ConfigureServices() method.
Attempted to register: MyNamespace.IMyService
```

**Cause:**
Your module is trying to register a service in `Initialize()` or `PostInitialize()` after the container has been built.

**Solution:**
Move the service registration to `ConfigureServices()`:

```csharp
// ❌ WRONG
public override void Initialize()
{
    IocManager.Register<IMyService, MyServiceImpl>();
}

// ✅ CORRECT
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
}
```

**How to Find:**
1. Look at the stack trace to find which module is registering the service
2. Search for `IocManager.Register` in `Initialize()` or `PostInitialize()` methods
3. Move all registrations to `ConfigureServices()`

---

### Error: "Container is already built and cannot be rebuilt"

**Full Error Message:**
```
Abp.AbpException: Container is already built and cannot be rebuilt.
BuildContainer() can only be called once.
```

**Cause:**
`BuildContainer()` is being called multiple times, possibly in custom code.

**Solution:**
Remove any custom calls to `BuildContainer()`. The framework handles this automatically:

```csharp
// ❌ WRONG - Don't call BuildContainer manually
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
    ((IocManager)IocManager).BuildContainer(); // Remove this!
}

// ✅ CORRECT - Let the framework build the container
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
}
```

**How to Find:**
1. Search your codebase for `BuildContainer()`
2. Remove any manual calls
3. The framework calls it automatically in `AddAbp()`

---

### Error: "Container must be built before calling Initialize()"

**Full Error Message:**
```
Abp.AbpInitializationException: Container must be built before calling Initialize().
Ensure that AddAbp() has been called in ConfigureServices() and the container
has been built before calling UseAbp() in Configure().
```

**Cause:**
Your `Startup.cs` is not properly configured for the two-phase lifecycle.

**Solution:**
Ensure your `Startup.cs` follows this pattern:

```csharp
public class Startup
{
    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        // ✅ MUST return IServiceProvider from AddAbp
        return services.AddAbp<MyStartupModule>(options =>
        {
            // Configuration...
        });
    }
    
    public void Configure(IApplicationBuilder app)
    {
        // ✅ Call UseAbp after AddAbp
        app.UseAbp();
        
        // Other middleware...
    }
}
```

**Common Mistakes:**

```csharp
// ❌ WRONG - Not returning IServiceProvider
public void ConfigureServices(IServiceCollection services)
{
    services.AddAbp<MyStartupModule>();
}

// ✅ CORRECT
public IServiceProvider ConfigureServices(IServiceCollection services)
{
    return services.AddAbp<MyStartupModule>();
}
```

---

## Service Registration Errors

### Error: "Service is not registered"

**Full Error Message:**
```
Abp.AbpException: Service MyNamespace.IMyService is not registered.
Ensure the service is registered in a module's ConfigureServices() method.
```

**Cause:**
The service was never registered, or it was registered in the wrong lifecycle method.

**Solution:**

1. **Check if service is registered:**
```csharp
public override void ConfigureServices()
{
    // Make sure this line exists
    IocManager.Register<IMyService, MyServiceImpl>();
}
```

2. **Check module dependencies:**
```csharp
// If IMyService is in AnotherModule, add dependency
[DependsOn(typeof(AnotherModule))]
public class MyModule : AbpModule
{
    // ...
}
```

3. **Check assembly registration:**
```csharp
public override void ConfigureServices()
{
    // Register assembly containing the service
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}
```

**How to Debug:**
1. Search for where `IMyService` should be registered
2. Verify the module containing the registration is loaded
3. Check module dependency order
4. Add logging to confirm `ConfigureServices()` is called:

```csharp
public override void ConfigureServices()
{
    Logger.Debug("Registering services for MyModule");
    IocManager.Register<IMyService, MyServiceImpl>();
    Logger.Debug($"IMyService registered: {IocManager.IsRegistered<IMyService>()}");
}
```

---

### Error: "Multiple implementations registered for service"

**Full Error Message:**
```
Autofac.Core.DependencyResolutionException: Multiple implementations are registered
for service 'MyNamespace.IMyService'. Please specify which one to use.
```

**Cause:**
Multiple modules are registering different implementations for the same interface.

**Solution:**

1. **Use named registrations:**
```csharp
// Module 1
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl1>(
        DependencyLifeStyle.Transient,
        "impl1"
    );
}

// Module 2
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl2>(
        DependencyLifeStyle.Transient,
        "impl2"
    );
}

// Usage
var service1 = IocManager.Resolve<IMyService>("impl1");
var service2 = IocManager.Resolve<IMyService>("impl2");
```

2. **Use ReplaceService:**
```csharp
public override void ConfigureServices()
{
    // Replace existing registration
    Configuration.ReplaceService<IMyService, MyNewServiceImpl>();
}
```

3. **Remove duplicate registrations:**
```csharp
// Only register in one module
public override void ConfigureServices()
{
    if (!IocManager.IsRegistered<IMyService>())
    {
        IocManager.Register<IMyService, MyServiceImpl>();
    }
}
```

---

### Error: "Circular dependency detected"

**Full Error Message:**
```
Autofac.Core.DependencyResolutionException: Circular dependency detected:
ServiceA -> ServiceB -> ServiceC -> ServiceA
```

**Cause:**
Services have circular dependencies in their constructors.

**Solution:**

1. **Refactor to remove circular dependency:**
```csharp
// ❌ WRONG - Circular dependency
public class ServiceA
{
    public ServiceA(ServiceB serviceB) { }
}

public class ServiceB
{
    public ServiceB(ServiceA serviceA) { }
}

// ✅ CORRECT - Extract common interface
public interface ISharedService { }

public class ServiceA
{
    public ServiceA(ISharedService sharedService) { }
}

public class ServiceB
{
    public ServiceB(ISharedService sharedService) { }
}
```

2. **Use property injection:**
```csharp
public class ServiceA
{
    public ServiceB ServiceB { get; set; }
    
    public ServiceA()
    {
        // Constructor doesn't require ServiceB
    }
}
```

3. **Use lazy resolution:**
```csharp
public class ServiceA
{
    private readonly Lazy<ServiceB> _serviceB;
    
    public ServiceA(Lazy<ServiceB> serviceB)
    {
        _serviceB = serviceB;
    }
    
    public void DoSomething()
    {
        _serviceB.Value.DoWork();
    }
}
```

---

## Service Resolution Errors

### Error: "Cannot resolve service in ConfigureServices"

**Symptom:**
Trying to resolve a service in `ConfigureServices()` throws an exception or returns null.

**Cause:**
The container hasn't been built yet, so services cannot be resolved.

**Solution:**
Move service resolution to `Initialize()`:

```csharp
// ❌ WRONG
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
    
    var service = IocManager.Resolve<IMyService>(); // Error!
    service.Configure();
}

// ✅ CORRECT
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
}

public override void Initialize()
{
    var service = IocManager.Resolve<IMyService>();
    service.Configure();
}
```

---

### Error: "Service lifetime mismatch"

**Full Error Message:**
```
InvalidOperationException: Cannot consume scoped service 'IMyService' from singleton 'MyConsumer'.
```

**Cause:**
A singleton service is trying to inject a scoped or transient service.

**Solution:**

1. **Change consumer to scoped:**
```csharp
// Change from Singleton to Transient
IocManager.Register<IMyConsumer, MyConsumer>(DependencyLifeStyle.Transient);
```

2. **Use IIocResolver:**
```csharp
public class MyConsumer
{
    private readonly IIocResolver _iocResolver;
    
    public MyConsumer(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
    }
    
    public void DoWork()
    {
        using (var scope = _iocResolver.CreateScope())
        {
            var service = scope.Resolve<IMyService>();
            service.DoSomething();
        }
    }
}
```

---

## Module Dependency Errors

### Error: "Could not find a depended module"

**Full Error Message:**
```
Abp.AbpInitializationException: Could not find a depended module
MyNamespace.MyDependencyModule for MyNamespace.MyModule.
Ensure all module dependencies are properly referenced and loaded.
```

**Cause:**
A module declares a dependency that isn't loaded or doesn't exist.

**Solution:**

1. **Check DependsOn attribute:**
```csharp
// Make sure the dependency module exists
[DependsOn(typeof(MyDependencyModule))]
public class MyModule : AbpModule
{
    // ...
}
```

2. **Check assembly reference:**
- Ensure the assembly containing `MyDependencyModule` is referenced
- Check that the assembly is copied to the output directory

3. **Check module is public:**
```csharp
// ❌ WRONG - Internal module
internal class MyDependencyModule : AbpModule { }

// ✅ CORRECT - Public module
public class MyDependencyModule : AbpModule { }
```

---

### Error: "Circular dependency detected in module dependencies"

**Full Error Message:**
```
Abp.AbpInitializationException: Circular dependency detected in module dependencies.
Dependency chain: ModuleA -> ModuleB -> ModuleC -> ModuleA
```

**Cause:**
Modules have circular dependencies in their `DependsOn` attributes.

**Solution:**
Refactor module dependencies to remove the cycle:

```csharp
// ❌ WRONG - Circular dependency
[DependsOn(typeof(ModuleB))]
public class ModuleA : AbpModule { }

[DependsOn(typeof(ModuleC))]
public class ModuleB : AbpModule { }

[DependsOn(typeof(ModuleA))]
public class ModuleC : AbpModule { }

// ✅ CORRECT - Linear dependency
public class ModuleA : AbpModule { }

[DependsOn(typeof(ModuleA))]
public class ModuleB : AbpModule { }

[DependsOn(typeof(ModuleB))]
public class ModuleC : AbpModule { }
```

---

## Initialization Errors

### Error: "NullReferenceException in Initialize"

**Symptom:**
`NullReferenceException` when accessing `IocManager` or `Configuration` in `Initialize()`.

**Cause:**
The properties haven't been set yet, or you're calling `Initialize()` manually.

**Solution:**

1. **Don't call Initialize manually:**
```csharp
// ❌ WRONG
var module = new MyModule();
module.Initialize(); // Properties not set!

// ✅ CORRECT - Let framework call it
// Framework sets properties before calling Initialize()
```

2. **Check property access:**
```csharp
public override void Initialize()
{
    // ✅ These should never be null if called by framework
    if (IocManager == null)
    {
        throw new AbpException("IocManager is null - Initialize called incorrectly");
    }
    
    var service = IocManager.Resolve<IMyService>();
}
```

---

### Error: "Configuration property throws exception"

**Symptom:**
Accessing `Configuration.Modules.MyModule()` throws an exception in `ConfigureServices()`.

**Cause:**
Some configuration properties try to resolve services internally.

**Solution:**

1. **Use simple configuration in ConfigureServices:**
```csharp
public override void ConfigureServices()
{
    // ✅ Simple property access is OK
    Configuration.Modules.MyModule().EnableFeature = true;
}
```

2. **Move complex configuration to Initialize:**
```csharp
public override void Initialize()
{
    // ✅ Configuration that resolves services
    var config = Configuration.Modules.MyModule();
    config.ConfigureWithResolvedServices();
}
```

---

## Runtime Errors

### Error: "Service disposed too early"

**Symptom:**
`ObjectDisposedException` when using a service.

**Cause:**
Service lifetime is too short, or you're holding a reference after disposal.

**Solution:**

1. **Use correct lifetime:**
```csharp
// Change from Transient to Scoped or Singleton
IocManager.Register<IMyService, MyServiceImpl>(DependencyLifeStyle.Scoped);
```

2. **Use proper scoping:**
```csharp
public void DoWork()
{
    using (var scope = IocManager.CreateScope())
    {
        var service = scope.Resolve<IMyService>();
        service.DoSomething();
    } // Service disposed here
}
```

3. **Don't store transient services:**
```csharp
// ❌ WRONG
private IMyService _service;

public void Initialize()
{
    _service = IocManager.Resolve<IMyService>(); // May be disposed
}

// ✅ CORRECT
public void DoWork()
{
    var service = IocManager.Resolve<IMyService>(); // Resolve when needed
    service.DoSomething();
}
```

---

### Error: "DbContext disposed"

**Symptom:**
`ObjectDisposedException: Cannot access a disposed object. Object name: 'MyDbContext'.`

**Cause:**
DbContext was disposed before you finished using it.

**Solution:**

1. **Use proper scoping:**
```csharp
public void DoWork()
{
    using (var uow = IocManager.Resolve<IUnitOfWorkManager>().Begin())
    {
        var repository = IocManager.Resolve<IMyRepository>();
        repository.Insert(new MyEntity());
        
        uow.Complete();
    }
}
```

2. **Don't store DbContext:**
```csharp
// ❌ WRONG
private MyDbContext _dbContext;

public void Initialize()
{
    _dbContext = IocManager.Resolve<MyDbContext>();
}

// ✅ CORRECT
public void DoWork()
{
    using (var dbContext = IocManager.Resolve<MyDbContext>())
    {
        // Use dbContext
    }
}
```

---

## Performance Issues

### Issue: "Slow startup time"

**Symptom:**
Application takes a long time to start.

**Diagnosis:**

1. **Add timing logs:**
```csharp
public override void ConfigureServices()
{
    var sw = Stopwatch.StartNew();
    
    // Your registrations...
    
    Logger.Info($"ConfigureServices completed in {sw.ElapsedMilliseconds}ms");
}
```

2. **Profile module loading:**
```csharp
public override void Initialize()
{
    var sw = Stopwatch.StartNew();
    
    // Your initialization...
    
    Logger.Info($"Initialize completed in {sw.ElapsedMilliseconds}ms");
}
```

**Solutions:**

1. **Lazy load plugins:**
```csharp
public override void ConfigureServices()
{
    // Don't load all plugins at startup
    Configuration.Modules.AbpPlugIns().LazyLoad = true;
}
```

2. **Defer non-critical initialization:**
```csharp
public override void PostInitialize()
{
    // Move non-critical initialization here
    Task.Run(() => InitializeNonCriticalServices());
}
```

3. **Optimize assembly scanning:**
```csharp
public override void ConfigureServices()
{
    // Only scan specific assemblies
    IocManager.RegisterAssemblyByConvention(
        typeof(MyModule).Assembly,
        new ConventionalRegistrationConfig
        {
            InstallInstallers = false // Skip installers if not needed
        }
    );
}
```

---

### Issue: "High memory usage"

**Symptom:**
Application uses more memory than expected.

**Diagnosis:**

1. **Check for memory leaks:**
```csharp
// Use memory profiler to identify leaks
// Common causes: event handlers not unsubscribed, static references
```

2. **Check service lifetimes:**
```csharp
// Too many Singleton services can increase memory
// Review and change to Transient or Scoped where appropriate
```

**Solutions:**

1. **Use appropriate lifetimes:**
```csharp
// ❌ WRONG - Singleton for stateful service
IocManager.Register<IMyService, MyServiceImpl>(DependencyLifeStyle.Singleton);

// ✅ CORRECT - Transient for stateful service
IocManager.Register<IMyService, MyServiceImpl>(DependencyLifeStyle.Transient);
```

2. **Dispose resources properly:**
```csharp
public class MyService : IMyService, IDisposable
{
    private readonly List<byte[]> _cache = new List<byte[]>();
    
    public void Dispose()
    {
        _cache.Clear(); // Clean up resources
    }
}
```

---

## Debugging Tips

### Enable Detailed Logging

```csharp
public override void ConfigureServices()
{
    // Enable debug logging
    Configuration.Logging.LogLevel = LogLevel.Debug;
}
```

### Check Container State

```csharp
public override void Initialize()
{
    var iocManager = (IocManager)IocManager;
    Logger.Debug($"Container built: {iocManager.IsContainerBuilt}");
    Logger.Debug($"Registered services: {iocManager.IocContainer.ComponentRegistry.Registrations.Count()}");
}
```

### Verify Service Registration

```csharp
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
    
    // Verify registration
    if (!IocManager.IsRegistered<IMyService>())
    {
        throw new AbpException("IMyService not registered!");
    }
}
```

### Test Module in Isolation

```csharp
[Fact]
public void Should_Register_Services()
{
    // Arrange
    var iocManager = new IocManager();
    var module = new MyModule { IocManager = iocManager };
    
    // Act
    module.ConfigureServices();
    iocManager.BuildContainer();
    
    // Assert
    iocManager.IsRegistered<IMyService>().ShouldBeTrue();
}
```

---

## Getting More Help

If you're still stuck after trying these solutions:

1. **Check the logs** - Enable debug logging and review the output
2. **Review the migration guide** - [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md)
3. **Check code examples** - [EXAMPLES.md](./EXAMPLES.md)
4. **Review lifecycle documentation** - [TWO_PHASE_LIFECYCLE.md](./TWO_PHASE_LIFECYCLE.md)
5. **Open an issue** - Include:
   - Full error message and stack trace
   - Relevant module code
   - Steps to reproduce
   - ABP version
   - .NET version

## Common Patterns Checklist

Use this checklist to avoid common issues:

- [ ] All `IocManager.Register()` calls are in `ConfigureServices()`
- [ ] All `RegisterAssemblyByConvention()` calls are in `ConfigureServices()`
- [ ] No service resolution in `ConfigureServices()`
- [ ] All service resolution is in `Initialize()` or `PostInitialize()`
- [ ] Module dependencies are correctly declared with `[DependsOn]`
- [ ] No circular module dependencies
- [ ] No circular service dependencies
- [ ] Proper service lifetimes (Singleton/Scoped/Transient)
- [ ] DbContext used with proper scoping
- [ ] Resources disposed properly
- [ ] `Startup.cs` returns `IServiceProvider` from `ConfigureServices()`
- [ ] `app.UseAbp()` called in `Configure()` method
