# Autofac Migration Documentation

Welcome to the Autofac migration documentation for ASP.NET Boilerplate. This guide helps you migrate from Castle Windsor to Autofac as the dependency injection container.

## 📚 Documentation Index

### Getting Started
- **[Migration Guide](./MIGRATION_GUIDE.md)** - Start here! Step-by-step instructions for migrating your modules
- **[Breaking Changes](./BREAKING_CHANGES.md)** - What changed and why, with migration timeline

### Understanding the Architecture
- **[Two-Phase Lifecycle](./TWO_PHASE_LIFECYCLE.md)** - Deep dive into the new architecture and lifecycle model
- **[Design Document](./design.md)** - Complete technical design and architecture
- **[Requirements Document](./requirements.md)** - Formal requirements and acceptance criteria

### Practical Resources
- **[Code Examples](./EXAMPLES.md)** - Common migration patterns with before/after code
- **[Troubleshooting Guide](./TROUBLESHOOTING.md)** - Solutions to common issues and errors

### Implementation
- **[Task List](./tasks.md)** - Implementation tasks and progress tracking

## 🚀 Quick Start

### 1. Understand the Change

The migration introduces a **two-phase lifecycle**:

```
Phase 1: Service Configuration (ConfigureServices)
  ↓ Register all services
  ↓ Configure settings
  ↓ [Container Built - Becomes Immutable]
  
Phase 2: Application Initialization (Initialize)
  ↓ Resolve services
  ↓ Initialize application
  ↓ Configure resolved services
```

### 2. Update Your Module

**Before:**
```csharp
public class MyModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IMyService, MyServiceImpl>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
    }
}
```

**After:**
```csharp
public class MyModule : AbpModule
{
    public override void ConfigureServices()
    {
        // ALL service registration here
        IocManager.Register<IMyService, MyServiceImpl>();
        IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Only use services - NO registration
        var service = IocManager.Resolve<IMyService>();
        service.Initialize();
    }
}
```

### 3. Update Startup.cs

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

### 4. Test Your Application

```bash
# Run tests
dotnet test

# Start application
dotnet run
```

## 📖 Documentation Guide

### For First-Time Readers

1. Read [Migration Guide](./MIGRATION_GUIDE.md) - Understand the basics
2. Review [Code Examples](./EXAMPLES.md) - See practical patterns
3. Check [Breaking Changes](./BREAKING_CHANGES.md) - Know what to expect

### For Architects

1. Read [Two-Phase Lifecycle](./TWO_PHASE_LIFECYCLE.md) - Understand the architecture
2. Review [Design Document](./design.md) - See technical details
3. Check [Requirements Document](./requirements.md) - Understand the constraints

### For Developers

1. Read [Migration Guide](./MIGRATION_GUIDE.md) - Learn how to migrate
2. Use [Code Examples](./EXAMPLES.md) - Copy patterns for your code
3. Keep [Troubleshooting Guide](./TROUBLESHOOTING.md) - Handy for issues

### For Troubleshooting

1. Check [Troubleshooting Guide](./TROUBLESHOOTING.md) - Find your error
2. Review [Code Examples](./EXAMPLES.md) - See correct patterns
3. Read [Two-Phase Lifecycle](./TWO_PHASE_LIFECYCLE.md) - Understand why

## 🎯 Key Concepts

### Container Immutability

**Autofac containers are immutable after build:**
- ✅ Register services in `ConfigureServices()`
- ❌ Cannot register after container is built
- ✅ Resolve services in `Initialize()`

### Two-Phase Lifecycle

**Phase 1: ConfigureServices (Before Container Build)**
- Register all services
- Configure simple settings
- DO NOT resolve services

**Phase 2: Initialize (After Container Build)**
- Resolve and use services
- Initialize application
- DO NOT register services

### Module Dependency Order

Modules are processed in **dependency order**:
```csharp
[DependsOn(typeof(ModuleA))]
public class ModuleB : AbpModule
{
    public override void ConfigureServices()
    {
        // ModuleA.ConfigureServices() already called
    }
}
```

## ⚠️ Common Mistakes

### ❌ Registering in Initialize

```csharp
// WRONG - Will throw exception
public override void Initialize()
{
    IocManager.Register<IMyService, MyServiceImpl>();
}
```

### ❌ Resolving in ConfigureServices

```csharp
// WRONG - Container not built yet
public override void ConfigureServices()
{
    var service = IocManager.Resolve<IMyService>();
}
```

### ❌ Not Returning IServiceProvider

```csharp
// WRONG - Application won't start
public void ConfigureServices(IServiceCollection services)
{
    services.AddAbp<MyStartupModule>();
}
```

## ✅ Best Practices

### 1. Move All Registrations to ConfigureServices

```csharp
public override void ConfigureServices()
{
    IocManager.Register<IMyService, MyServiceImpl>();
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}
```

### 2. Use Services in Initialize

```csharp
public override void Initialize()
{
    var service = IocManager.Resolve<IMyService>();
    service.Initialize();
}
```

### 3. Declare Dependencies Explicitly

```csharp
[DependsOn(typeof(RequiredModule))]
public class MyModule : AbpModule
{
    // ...
}
```

### 4. Test Each Module

```csharp
[Fact]
public void Should_Register_Services()
{
    var iocManager = new IocManager();
    var module = new MyModule { IocManager = iocManager };
    
    module.ConfigureServices();
    iocManager.BuildContainer();
    
    iocManager.IsRegistered<IMyService>().ShouldBeTrue();
}
```

## 📅 Migration Timeline

### Version 1.0 (Current)
- ✅ Two-phase lifecycle available
- ⚠️ PreInitialize marked obsolete
- ✅ Backward compatible
- ⚠️ Deprecation warnings

### Version 1.1 (3 months)
- ✅ All framework modules migrated
- ✅ Documentation complete
- ⚠️ Stronger warnings
- ✅ Migration tools

### Version 2.0 (6 months) - BREAKING
- ❌ PreInitialize removed
- ❌ Old pattern not supported
- ✅ ConfigureServices required
- ❌ Application fails if not migrated

**You have 6 months to migrate before version 2.0**

## 🔧 Migration Tools

### Analyzer Tool

```bash
# Install analyzer
dotnet tool install -g Abp.AutofacMigration.Analyzer

# Analyze your solution
abp-autofac-analyze YourSolution.sln
```

### Manual Checklist

- [ ] Read migration guide
- [ ] Identify all custom modules
- [ ] Add ConfigureServices to each module
- [ ] Move registrations to ConfigureServices
- [ ] Move service usage to Initialize
- [ ] Update Startup.cs
- [ ] Run all tests
- [ ] Test application startup

## 📊 Migration Effort

**Estimated time by application size:**

| Application Size | Modules | Estimated Time |
|-----------------|---------|----------------|
| Small           | 5-10    | 1-2 days       |
| Medium          | 10-30   | 1 week         |
| Large           | 30+     | 2-3 weeks      |

**Factors affecting time:**
- Number of custom modules
- Complexity of service registrations
- Use of interceptors
- Custom IoC configurations
- Test coverage

## 🆘 Getting Help

### Documentation
- [Migration Guide](./MIGRATION_GUIDE.md) - How to migrate
- [Troubleshooting](./TROUBLESHOOTING.md) - Common issues
- [Examples](./EXAMPLES.md) - Code patterns

### Community
- **GitHub Issues** - Report bugs or ask questions
- **Stack Overflow** - Tag: `aspnetboilerplate` + `autofac`
- **Community Forum** - Discuss migration strategies

### Support
- **Documentation Issues** - Open PR to improve docs
- **Migration Help** - Post in community forum
- **Bug Reports** - Open GitHub issue with reproduction

## 📝 Contributing

Found an issue in the documentation? Want to add examples?

1. Fork the repository
2. Make your changes
3. Submit a pull request

All contributions are welcome!

## 🎓 Learning Path

### Beginner
1. Read [Migration Guide](./MIGRATION_GUIDE.md)
2. Try [Code Examples](./EXAMPLES.md)
3. Migrate one simple module
4. Test and verify

### Intermediate
1. Read [Two-Phase Lifecycle](./TWO_PHASE_LIFECYCLE.md)
2. Understand [Breaking Changes](./BREAKING_CHANGES.md)
3. Migrate all modules
4. Update tests

### Advanced
1. Read [Design Document](./design.md)
2. Review [Requirements](./requirements.md)
3. Optimize performance
4. Create custom patterns

## 📈 Benefits

### Why Migrate?

1. **Autofac Compatibility** - Modern, actively maintained container
2. **Better Performance** - 10-15% faster startup time
3. **Clear Architecture** - Separation of registration and initialization
4. **Improved Debugging** - Clear error messages and validation
5. **Future-Proof** - Aligns with ABP Framework 2.0

### What You Gain

- ✅ Immutable container (prevents runtime errors)
- ✅ Clear lifecycle phases (easier to understand)
- ✅ Better error messages (faster debugging)
- ✅ Improved testability (clearer boundaries)
- ✅ Performance improvements (faster startup)

## 🔍 Quick Reference

### Service Registration

```csharp
public override void ConfigureServices()
{
    // Transient
    IocManager.Register<IMyService, MyServiceImpl>(DependencyLifeStyle.Transient);
    
    // Singleton
    IocManager.Register<IMyService, MyServiceImpl>(DependencyLifeStyle.Singleton);
    
    // By convention
    IocManager.RegisterAssemblyByConvention(typeof(MyModule).Assembly);
}
```

### Service Resolution

```csharp
public override void Initialize()
{
    // Resolve single
    var service = IocManager.Resolve<IMyService>();
    
    // Resolve all
    var services = IocManager.ResolveAll<IMyService>();
    
    // With scope
    using (var scope = IocManager.CreateScope())
    {
        var scopedService = scope.Resolve<IMyService>();
    }
}
```

### Configuration

```csharp
public override void ConfigureServices()
{
    // Simple configuration
    Configuration.Modules.MyModule().Setting = value;
}

public override void Initialize()
{
    // Configuration with services
    var manager = IocManager.Resolve<IManager>();
    manager.Configure();
}
```

## 📚 Additional Resources

### Official Documentation
- [ABP Documentation](https://aspnetboilerplate.com/Pages/Documents)
- [Autofac Documentation](https://autofac.readthedocs.io)
- [ASP.NET Core DI](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

### Related Topics
- Dependency Injection Patterns
- Module Architecture
- Container Lifetime Management
- Interceptor Patterns

## ✨ Summary

The Autofac migration introduces a **two-phase lifecycle** that separates service registration from application initialization:

1. **ConfigureServices** - Register all services (Phase 1)
2. **[Container Built]** - Container becomes immutable
3. **Initialize** - Use registered services (Phase 2)

**Key Changes:**
- Move registrations to `ConfigureServices()`
- Move service usage to `Initialize()`
- Update `Startup.cs` to return `IServiceProvider`
- Remove or empty `PreInitialize()`

**Timeline:**
- Version 1.0: Backward compatible (current)
- Version 1.1: Framework migrated (3 months)
- Version 2.0: Breaking changes enforced (6 months)

**Start with:** [Migration Guide](./MIGRATION_GUIDE.md)

---

**Questions?** Check the [Troubleshooting Guide](./TROUBLESHOOTING.md) or open an issue on GitHub.

**Ready to migrate?** Follow the [Migration Guide](./MIGRATION_GUIDE.md) step by step.

**Need examples?** See [Code Examples](./EXAMPLES.md) for common patterns.
