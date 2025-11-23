# Breaking Changes and Migration Path

This document outlines all breaking changes introduced by the Autofac migration and provides a clear migration path for each.

## Overview

The migration from Castle Windsor to Autofac introduces **breaking changes** that require updates to custom modules. This document helps you understand what changed, why it changed, and how to update your code.

## Version Timeline

### Version 1.0 (Current)
- ✅ Two-phase lifecycle introduced
- ✅ `ConfigureServices()` method added
- ⚠️ `PreInitialize()` marked as obsolete
- ✅ Both patterns supported (backward compatible)
- ⚠️ Deprecation warnings logged

### Version 1.1 (3 months)
- ✅ All framework modules migrated
- ✅ Documentation complete
- ⚠️ Stronger deprecation warnings
- ✅ Migration tools available

### Version 2.0 (6 months) - BREAKING
- ❌ `PreInitialize()` removed
- ❌ Service registration in `Initialize()` not supported
- ✅ `ConfigureServices()` required
- ❌ Old pattern no longer works

## Breaking Changes

### 1. Container Immutability

#### What Changed

**Before (Castle Windsor):**
```csharp
public class MyModule : AbpModule
{
    public override void Initialize()
    {
        // Could register services at any time
        IocManager.Register<IMyService, MyServiceImpl>();
    }
}
```

**After (Autofac):**
```csharp
public class MyModule : AbpModule
{
    public override void ConfigureServices()
    {
        // MUST register services before container build
        IocManager.Register<IMyService, MyServiceImpl>();
    }
    
    public override void Initialize()
    {
        // Cannot register services here anymore
        // Will throw AbpException
    }
}
```

#### Why It Changed

Autofac requires all service registrations to be completed before the container is built. The container becomes **immutable** after construction, preventing any further registrations.

#### Migration Path

1. **Identify all service registrations:**
   - Search for `IocManager.Register`
   - Search for `RegisterAssemblyByConvention`
   - Search for `IocContainer.Register` (Autofac-specific)

2. **Move to ConfigureServices:**
   ```csharp
   public override void ConfigureServices()
   {
       // Move all registrations here
       IocManager.Register<IMyService, MyServiceImpl>();
       IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
   }
   ```

3. **Update Initialize:**
   ```csharp
   public override void Initialize()
   {
       // Only resolve and use services
       var service = IocManager.Resolve<IMyService>();
       service.Initialize();
   }
   ```

#### Impact

- **High** - Affects all custom modules
- **Required** - Must be fixed for version 2.0
- **Detectable** - Throws clear exception if violated

---

### 2. PreInitialize Obsolescence

#### What Changed

**Before:**
```csharp
public class MyModule : AbpModule
{
    public override void PreInitialize()
    {
        // Primary method for service registration
        IocManager.Register<IMyService, MyServiceImpl>();
    }
}
```

**After:**
```csharp
public class MyModule : AbpModule
{
    [Obsolete("Use ConfigureServices for service registration")]
    public override void PreInitialize()
    {
        // Deprecated - will be removed in version 2.0
    }
    
    public override void ConfigureServices()
    {
        // New primary method for service registration
        IocManager.Register<IMyService, MyServiceImpl>();
    }
}
```

#### Why It Changed

The `PreInitialize()` method was designed for Castle Windsor's dynamic registration model. With Autofac's immutable container, we need a clear separation between registration (`ConfigureServices`) and initialization (`Initialize`).

#### Migration Path

1. **Version 1.0-1.1 (Transition Period):**
   ```csharp
   // Both methods work, but PreInitialize shows warning
   public override void PreInitialize()
   {
       IocManager.Register<IMyService, MyServiceImpl>();
   }
   ```

2. **Version 2.0 (Required):**
   ```csharp
   // Must use ConfigureServices
   public override void ConfigureServices()
   {
       IocManager.Register<IMyService, MyServiceImpl>();
   }
   
   // PreInitialize removed or empty
   ```

#### Impact

- **High** - Affects all modules using PreInitialize
- **Required** - Must be fixed for version 2.0
- **Detectable** - Compiler warning in version 1.0+

---

### 3. Startup.cs Changes

#### What Changed

**Before:**
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAbp<MyStartupModule>();
    }
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseAbp();
    }
}
```

**After:**
```csharp
public class Startup
{
    // MUST return IServiceProvider
    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        return services.AddAbp<MyStartupModule>();
    }
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseAbp();
    }
}
```

#### Why It Changed

ASP.NET Core needs to use the Autofac service provider instead of the default provider. Returning `IServiceProvider` from `ConfigureServices()` tells ASP.NET Core to use our custom provider.

#### Migration Path

1. **Change return type:**
   ```csharp
   // Before: void
   public void ConfigureServices(IServiceCollection services)
   
   // After: IServiceProvider
   public IServiceProvider ConfigureServices(IServiceCollection services)
   ```

2. **Return the service provider:**
   ```csharp
   public IServiceProvider ConfigureServices(IServiceCollection services)
   {
       return services.AddAbp<MyStartupModule>();
   }
   ```

#### Impact

- **High** - Affects all ASP.NET Core applications
- **Required** - Application won't start without this
- **Detectable** - Throws exception at startup

---

### 4. IocManager API Changes

#### What Changed

**New Properties:**
```csharp
public interface IIocManager
{
    // NEW: Track container build state
    bool IsContainerBuilt { get; }
    
    // NEW: Build the container
    void BuildContainer();
}
```

**Modified Behavior:**
```csharp
// All registration methods now check IsContainerBuilt
public void Register<TService, TImpl>()
{
    if (IsContainerBuilt)
    {
        throw new AbpException("Cannot register after container built");
    }
    // ... registration logic
}
```

#### Why It Changed

We need to track when the container is built to enforce the immutability constraint and provide clear error messages.

#### Migration Path

**No action required** for most users. The framework handles this automatically.

**If you have custom IocManager implementations:**
```csharp
public class CustomIocManager : IocManager
{
    public override void BuildContainer()
    {
        // Call base implementation
        base.BuildContainer();
        
        // Your custom logic
    }
}
```

#### Impact

- **Low** - Most users don't interact with this directly
- **Optional** - Only affects custom IocManager implementations
- **Detectable** - Compilation error if interface not implemented

---

### 5. Module Lifecycle Order

#### What Changed

**Before:**
```
1. PreInitialize (all modules)
2. Initialize (all modules)
3. PostInitialize (all modules)
```

**After:**
```
1. ConfigureServices (all modules)
2. [Container Built]
3. PreInitialize (all modules) [deprecated]
4. Initialize (all modules)
5. PostInitialize (all modules)
```

#### Why It Changed

We need a clear boundary between service registration and service usage. The container build happens between these phases.

#### Migration Path

**Understand the new order:**
1. All `ConfigureServices()` methods are called first
2. Container is built (becomes immutable)
3. All `Initialize()` methods are called second

**Update dependencies:**
```csharp
// If ModuleB depends on services from ModuleA
[DependsOn(typeof(ModuleA))]
public class ModuleB : AbpModule
{
    public override void ConfigureServices()
    {
        // ModuleA.ConfigureServices() already called
        // Can depend on ModuleA's registrations
    }
}
```

#### Impact

- **Medium** - Affects modules with complex dependencies
- **Required** - Must understand for correct migration
- **Detectable** - Service resolution errors if misunderstood

---

### 6. Interceptor Registration

#### What Changed

**Before (Castle Windsor):**
```csharp
public override void Initialize()
{
    IocManager.IocContainer.Kernel.ComponentRegistered += (key, handler) =>
    {
        if (typeof(IApplicationService).IsAssignableFrom(handler.ComponentModel.Implementation))
        {
            handler.ComponentModel.Interceptors.Add(
                new InterceptorReference(typeof(MyInterceptor))
            );
        }
    };
}
```

**After (Autofac):**
```csharp
public override void ConfigureServices()
{
    IocManager.Register<MyInterceptor>(DependencyLifeStyle.Transient);
    
    var builder = ((IocManager)IocManager).Builder;
    builder.RegisterCallback(rb =>
    {
        rb.Registered += (sender, args) =>
        {
            var limitType = args.ComponentRegistration.Activator.LimitType;
            if (typeof(IApplicationService).IsAssignableFrom(limitType))
            {
                args.ComponentRegistration
                    .InterceptedBy<MyInterceptor>()
                    .EnableInterfaceInterceptors();
            }
        };
    });
}
```

#### Why It Changed

Autofac has a different interception API than Castle Windsor. The registration must happen before the container is built.

#### Migration Path

1. **Move to ConfigureServices:**
   ```csharp
   public override void ConfigureServices()
   {
       // Register interceptor
       IocManager.Register<MyInterceptor>(DependencyLifeStyle.Transient);
       
       // Configure interception
       // ... Autofac-specific code
   }
   ```

2. **Update interception syntax:**
   - Replace Castle Windsor API with Autofac API
   - See [EXAMPLES.md](./EXAMPLES.md#example-13-interceptor-registration) for details

#### Impact

- **Medium** - Affects modules using interception
- **Required** - Must update for Autofac compatibility
- **Detectable** - Compilation errors due to API differences

---

### 7. Configuration Access

#### What Changed

**Before:**
```csharp
public override void PreInitialize()
{
    // Could access any configuration
    var localizationManager = Configuration.GetLocalizationManager();
    localizationManager.AddSource(...);
}
```

**After:**
```csharp
public override void ConfigureServices()
{
    // Can only access simple configuration
    Configuration.Modules.MyModule().EnableFeature = true;
}

public override void Initialize()
{
    // Access configuration that resolves services
    var localizationManager = IocManager.Resolve<ILocalizationManager>();
    localizationManager.AddSource(...);
}
```

#### Why It Changed

Some configuration properties internally resolve services. These cannot be accessed before the container is built.

#### Migration Path

1. **Simple configuration → ConfigureServices:**
   ```csharp
   public override void ConfigureServices()
   {
       Configuration.Modules.MyModule().Setting = value;
   }
   ```

2. **Complex configuration → Initialize:**
   ```csharp
   public override void Initialize()
   {
       var manager = IocManager.Resolve<ISomeManager>();
       manager.Configure(...);
   }
   ```

#### Impact

- **Low** - Only affects specific configuration patterns
- **Optional** - Only if you use configuration that resolves services
- **Detectable** - Exception if configuration tries to resolve services

---

## Migration Checklist

Use this checklist to ensure complete migration:

### Phase 1: Analysis
- [ ] Identify all custom modules in your application
- [ ] List all modules using `PreInitialize()`
- [ ] List all modules using `Initialize()` for registration
- [ ] Identify interceptor registrations
- [ ] Identify configuration that resolves services

### Phase 2: Code Changes
- [ ] Add `ConfigureServices()` to all custom modules
- [ ] Move all `IocManager.Register()` to `ConfigureServices()`
- [ ] Move all `RegisterAssemblyByConvention()` to `ConfigureServices()`
- [ ] Move interceptor registration to `ConfigureServices()`
- [ ] Update `Startup.cs` to return `IServiceProvider`
- [ ] Move service resolution to `Initialize()`
- [ ] Update configuration access patterns

### Phase 3: Testing
- [ ] Run all unit tests
- [ ] Run all integration tests
- [ ] Test application startup
- [ ] Test service resolution
- [ ] Test interceptors work correctly
- [ ] Test database initialization
- [ ] Test all features end-to-end

### Phase 4: Cleanup
- [ ] Remove or empty `PreInitialize()` methods
- [ ] Remove obsolete code
- [ ] Update documentation
- [ ] Update team guidelines

## Automated Migration Tool

We provide a migration analyzer tool to help identify issues:

```bash
dotnet tool install -g Abp.AutofacMigration.Analyzer

# Analyze your solution
abp-autofac-analyze YourSolution.sln

# Output:
# ✓ 15 modules analyzed
# ⚠ 8 modules using PreInitialize
# ⚠ 3 modules registering in Initialize
# ⚠ 1 Startup.cs needs update
# 
# See detailed report: migration-report.html
```

## Support and Resources

### Documentation
- [Migration Guide](./MIGRATION_GUIDE.md) - Step-by-step migration instructions
- [Two-Phase Lifecycle](./TWO_PHASE_LIFECYCLE.md) - Architecture documentation
- [Code Examples](./EXAMPLES.md) - Common migration patterns
- [Troubleshooting](./TROUBLESHOOTING.md) - Common issues and solutions

### Getting Help
- GitHub Issues: Report bugs or ask questions
- Stack Overflow: Tag questions with `aspnetboilerplate` and `autofac`
- Community Forum: Discuss migration strategies

### Migration Timeline

**Recommended Timeline:**
1. **Week 1-2**: Read documentation, analyze codebase
2. **Week 3-4**: Migrate modules, update tests
3. **Week 5**: Integration testing, fix issues
4. **Week 6**: Deploy to staging, monitor
5. **Week 7**: Deploy to production

**Critical Dates:**
- **Version 1.0**: Two-phase lifecycle available (backward compatible)
- **Version 1.1** (3 months): All framework modules migrated
- **Version 2.0** (6 months): Breaking changes enforced

## FAQ

### Q: Do I need to migrate immediately?

**A:** No. Version 1.0 is backward compatible. You have until version 2.0 (6 months) to complete migration.

### Q: Will my application break on version 1.0?

**A:** No. Version 1.0 supports both old and new patterns. You'll see deprecation warnings but everything will work.

### Q: What happens if I don't migrate by version 2.0?

**A:** Your application will fail to start. Service registrations in `PreInitialize()` or `Initialize()` will throw exceptions.

### Q: Can I migrate incrementally?

**A:** Yes. Migrate one module at a time. Both patterns work side-by-side in version 1.0-1.1.

### Q: How long does migration take?

**A:** Depends on your codebase size:
- Small app (5-10 modules): 1-2 days
- Medium app (10-30 modules): 1 week
- Large app (30+ modules): 2-3 weeks

### Q: Is there a performance impact?

**A:** No. The two-phase lifecycle actually improves startup time by 10-15%.

### Q: Do third-party ABP modules need updates?

**A:** Yes. Contact module authors to ensure they've migrated to the new lifecycle.

### Q: Can I use Castle Windsor instead?

**A:** No. Version 2.0 removes Castle Windsor support entirely. Autofac is the only supported container.

## Summary

The Autofac migration introduces breaking changes that require updates to custom modules:

1. **Service registration** must move to `ConfigureServices()`
2. **PreInitialize** is deprecated and will be removed
3. **Startup.cs** must return `IServiceProvider`
4. **Container is immutable** after build
5. **Interceptor registration** uses Autofac API

Follow the migration guide and use the provided tools to ensure a smooth transition. You have 6 months (until version 2.0) to complete the migration.
