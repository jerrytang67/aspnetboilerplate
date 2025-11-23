# Autofac Migration Guide

## Overview

This guide helps you migrate custom ABP modules from Castle Windsor to Autofac. The migration introduces a **two-phase lifecycle** that separates service registration from application initialization.

## What Changed?

### The Problem

Castle Windsor allows dynamic service registration after container construction, but Autofac requires all registrations to be completed **before** the container is built. This fundamental difference requires a new approach to module initialization.

### The Solution

We've introduced a two-phase lifecycle:

1. **Service Configuration Phase** (Before Container Build)
   - Register all services
   - Configure module settings
   - **DO NOT** resolve services

2. **Application Initialization Phase** (After Container Build)
   - Resolve and use services
   - Perform initialization logic
   - **DO NOT** register services

## Quick Start

### Before (Old Pattern)

```csharp
public class MyModule : AbpModule
{
    public override void PreInitialize()
    {
        // Service registration in PreInitialize
        IocManager.Register<IMyService, MyServiceImpl>();
        
        // Configuration
        Configuration.Modules.MyModule().EnableFeature = true;
    }

    public override void Initialize()
    {
        // More service registration
        IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
        
        // Using services
        var myService = IocManager.Resolve<IMyService>();
        myService.DoSomething();
    }
}
```

### After (New Pattern)

```csharp
public class MyModule : AbpModule
{
    public override void ConfigureServices()
    {
        // ALL service registration happens here
        IocManager.Register<IMyService, MyServiceImpl>();
        IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
        
        // Configuration that doesn't require resolved services
        Configuration.Modules.MyModule().EnableFeature = true;
    }

    [Obsolete("Use ConfigureServices for service registration")]
    public override void PreInitialize()
    {
        // Leave empty or remove
    }

    public override void Initialize()
    {
        // Only use resolved services - NO registration
        var myService = IocManager.Resolve<IMyService>();
        myService.DoSomething();
    }
}
```

## Migration Steps

### Step 1: Create ConfigureServices Method

Add a `ConfigureServices()` method to your module:

```csharp
public override void ConfigureServices()
{
    // Service registrations will go here
}
```

### Step 2: Move Service Registrations

Move **all** service registrations from `PreInitialize()` and `Initialize()` to `ConfigureServices()`:

**Move these patterns:**
- `IocManager.Register<TService, TImpl>()`
- `IocManager.RegisterAssemblyByConvention()`
- `IocManager.IocContainer.Register()` (Autofac-specific)
- Any configuration object registrations

**Example:**

```csharp
// BEFORE: In PreInitialize or Initialize
IocManager.Register<IMyRepository, MyRepository>(DependencyLifeStyle.Transient);
IocManager.Register<IMyService, MyService>();

// AFTER: In ConfigureServices
public override void ConfigureServices()
{
    IocManager.Register<IMyRepository, MyRepository>(DependencyLifeStyle.Transient);
    IocManager.Register<IMyService, MyService>();
}
```

### Step 3: Keep Service Usage in Initialize

Keep code that **uses** services in `Initialize()` or `PostInitialize()`:

```csharp
public override void Initialize()
{
    // Resolve and use services
    var myService = IocManager.Resolve<IMyService>();
    myService.Initialize();
    
    // Access configuration that requires resolved services
    var config = IocManager.Resolve<IMyConfiguration>();
    config.ApplySettings();
}
```

### Step 4: Handle Configuration Objects

**Configuration that doesn't need services** → `ConfigureServices()`

```csharp
public override void ConfigureServices()
{
    // Simple configuration
    Configuration.Modules.MyModule().EnableFeature = true;
    Configuration.Modules.MyModule().DefaultTimeout = 30;
}
```

**Configuration that needs resolved services** → `Initialize()`

```csharp
public override void Initialize()
{
    // Configuration using resolved services
    var localizationManager = IocManager.Resolve<ILocalizationManager>();
    localizationManager.AddSource(new MyLocalizationSource());
}
```

### Step 5: Remove or Empty PreInitialize

The `PreInitialize()` method is now obsolete:

```csharp
[Obsolete("Use ConfigureServices for service registration")]
public override void PreInitialize()
{
    // Leave empty or remove entirely
}
```

## Common Migration Patterns

### Pattern 1: Simple Service Registration

**Before:**
```csharp
public override void PreInitialize()
{
    IocManager.Register<IEmailSender, SmtpEmailSender>();
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    IocManager.Register<IEmailSender, SmtpEmailSender>();
}
```

### Pattern 2: Assembly Convention Registration

**Before:**
```csharp
public override void Initialize()
{
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}
```

### Pattern 3: Configuration with Service Resolution

**Before:**
```csharp
public override void PreInitialize()
{
    IocManager.Register<IMyConfig, MyConfig>();
    
    var config = IocManager.Resolve<IMyConfig>();
    config.Initialize();
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    // Only registration
    IocManager.Register<IMyConfig, MyConfig>();
}

public override void Initialize()
{
    // Service resolution and usage
    var config = IocManager.Resolve<IMyConfig>();
    config.Initialize();
}
```

### Pattern 4: Interceptor Registration

**Before:**
```csharp
public override void Initialize()
{
    IocManager.IocContainer.Kernel.ComponentRegistered += (key, handler) =>
    {
        if (typeof(IApplicationService).IsAssignableFrom(handler.ComponentModel.Implementation))
        {
            handler.ComponentModel.Interceptors.Add(new InterceptorReference(typeof(MyInterceptor)));
        }
    };
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    // Register interceptor
    IocManager.Register<MyInterceptor>(DependencyLifeStyle.Transient);
    
    // Configure interception (Autofac-specific)
    IocManager.IocContainer.RegisterCallback(rb =>
    {
        rb.Registered += (sender, args) =>
        {
            if (typeof(IApplicationService).IsAssignableFrom(args.ComponentRegistration.Activator.LimitType))
            {
                args.ComponentRegistration.InterceptedBy<MyInterceptor>();
            }
        };
    });
}
```

### Pattern 5: Conditional Registration

**Before:**
```csharp
public override void PreInitialize()
{
    if (Configuration.Modules.MyModule().UseCache)
    {
        IocManager.Register<ICacheProvider, RedisCacheProvider>();
    }
    else
    {
        IocManager.Register<ICacheProvider, MemoryCacheProvider>();
    }
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    // Same pattern - just move to ConfigureServices
    if (Configuration.Modules.MyModule().UseCache)
    {
        IocManager.Register<ICacheProvider, RedisCacheProvider>();
    }
    else
    {
        IocManager.Register<ICacheProvider, MemoryCacheProvider>();
    }
}
```

### Pattern 6: Database Initialization

**Before:**
```csharp
public override void Initialize()
{
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
    
    using (var dbContext = IocManager.Resolve<MyDbContext>())
    {
        dbContext.Database.Migrate();
    }
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    // Registration only
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}

public override void Initialize()
{
    // Database operations using resolved services
    using (var dbContext = IocManager.Resolve<MyDbContext>())
    {
        dbContext.Database.Migrate();
    }
}
```

## What Goes Where?

### ConfigureServices() - Before Container Build

✅ **DO:**
- Register services with `IocManager.Register()`
- Register assemblies with `RegisterAssemblyByConvention()`
- Configure simple settings that don't need services
- Register interceptors
- Register repositories
- Register application services

❌ **DON'T:**
- Resolve services with `IocManager.Resolve()`
- Access properties that resolve services internally
- Perform database operations
- Call external APIs
- Initialize services

### Initialize() - After Container Build

✅ **DO:**
- Resolve services with `IocManager.Resolve()`
- Use resolved services
- Perform database initialization
- Configure services that were registered
- Initialize external integrations

❌ **DON'T:**
- Register new services
- Call `IocManager.Register()`
- Call `RegisterAssemblyByConvention()`

## Breaking Changes

### 1. Container is Immutable After Build

**Error:**
```
AbpException: Cannot register services after container is built.
All service registrations must occur in the ConfigureServices() method.
```

**Solution:** Move the registration to `ConfigureServices()`.

### 2. Initialize Called Before Container Built

**Error:**
```
AbpInitializationException: Container must be built before calling Initialize().
Ensure that AddAbp() has been called in ConfigureServices().
```

**Solution:** Ensure your `Startup.cs` calls `services.AddAbp<TStartupModule>()` in `ConfigureServices()`.

### 3. Service Not Registered

**Error:**
```
AbpException: Service MyService is not registered.
Ensure the service is registered in a module's ConfigureServices() method.
```

**Solution:** Register the service in `ConfigureServices()` of your module or a dependency module.

## Testing Your Migration

### Unit Tests

Test that your module registers services correctly:

```csharp
[Fact]
public void Should_Register_Services_In_ConfigureServices()
{
    // Arrange
    var iocManager = new IocManager();
    var module = new MyModule { IocManager = iocManager };
    
    // Act
    module.ConfigureServices();
    iocManager.BuildContainer();
    
    // Assert
    iocManager.IsRegistered<IMyService>().ShouldBeTrue();
    var service = iocManager.Resolve<IMyService>();
    service.ShouldNotBeNull();
}
```

### Integration Tests

Test the complete startup flow:

```csharp
public class MyModuleIntegrationTests : AbpAspNetCoreIntegratedTestBase<MyModule>
{
    [Fact]
    public void Should_Resolve_Services_After_Startup()
    {
        // Services registered in ConfigureServices should be resolvable
        var myService = GetRequiredService<IMyService>();
        myService.ShouldNotBeNull();
    }
}
```

## Backward Compatibility

### Transition Period

During the transition period (versions 1.0-1.1):
- Both `PreInitialize()` and `ConfigureServices()` are supported
- `PreInitialize()` is marked with `[Obsolete]` attribute
- Warnings are logged when `PreInitialize()` is used

### Deprecation Warning

You'll see this warning if you still use `PreInitialize()`:

```
WARN: Module MyModule overrides PreInitialize(). This method is deprecated.
Please move service registrations to ConfigureServices().
See migration guide: [URL]
```

### Version 2.0 Breaking Change

In version 2.0:
- `PreInitialize()` will be removed
- `ConfigureServices()` will be required for service registration
- Modules not migrated will fail to start

## Checklist

Use this checklist to verify your migration:

- [ ] Created `ConfigureServices()` method
- [ ] Moved all `IocManager.Register()` calls to `ConfigureServices()`
- [ ] Moved all `RegisterAssemblyByConvention()` calls to `ConfigureServices()`
- [ ] Moved interceptor registration to `ConfigureServices()`
- [ ] Kept service resolution in `Initialize()` or `PostInitialize()`
- [ ] Removed or emptied `PreInitialize()`
- [ ] Tested that services are resolvable
- [ ] Tested that initialization logic works
- [ ] No registration errors during startup
- [ ] All unit tests pass
- [ ] All integration tests pass

## Getting Help

If you encounter issues during migration:

1. Check the [Troubleshooting Guide](./TROUBLESHOOTING.md)
2. Review the [Two-Phase Lifecycle Documentation](./TWO_PHASE_LIFECYCLE.md)
3. See [Code Examples](./EXAMPLES.md) for common scenarios
4. Open an issue on GitHub with:
   - Your module code
   - Error messages
   - Stack traces

## Next Steps

After migrating your modules:

1. Review the [Two-Phase Lifecycle Documentation](./TWO_PHASE_LIFECYCLE.md) for deeper understanding
2. Check [Code Examples](./EXAMPLES.md) for advanced scenarios
3. Read the [Troubleshooting Guide](./TROUBLESHOOTING.md) for common issues
4. Update your team's documentation
5. Plan migration timeline for production deployment
