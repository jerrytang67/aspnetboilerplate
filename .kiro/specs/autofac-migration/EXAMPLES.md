# Code Examples for Autofac Migration

This document provides comprehensive code examples for common scenarios when migrating to the two-phase lifecycle.

## Table of Contents

1. [Basic Module Migration](#basic-module-migration)
2. [Web Module Migration](#web-module-migration)
3. [Data Access Module Migration](#data-access-module-migration)
4. [Integration Module Migration](#integration-module-migration)
5. [Advanced Scenarios](#advanced-scenarios)

## Basic Module Migration

### Example 1: Simple Service Registration

**Before:**
```csharp
public class EmailModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IEmailSender, SmtpEmailSender>();
        IocManager.Register<IEmailTemplateProvider, DefaultEmailTemplateProvider>();
        
        Configuration.Modules.Email().DefaultFromAddress = "noreply@example.com";
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(EmailModule).Assembly);
    }
}
```

**After:**
```csharp
public class EmailModule : AbpModule
{
    public override void ConfigureServices()
    {
        // All service registration in one place
        IocManager.Register<IEmailSender, SmtpEmailSender>();
        IocManager.Register<IEmailTemplateProvider, DefaultEmailTemplateProvider>();
        IocManager.RegisterAssemblyByConvention(typeof(EmailModule).Assembly);
        
        // Simple configuration
        Configuration.Modules.Email().DefaultFromAddress = "noreply@example.com";
    }
    
    public override void Initialize()
    {
        // Test email connection
        var emailSender = IocManager.Resolve<IEmailSender>();
        Logger.Info($"Email sender initialized: {emailSender.GetType().Name}");
    }
}
```

### Example 2: Module with Dependencies

**Before:**
```csharp
[DependsOn(typeof(AbpKernelModule))]
public class NotificationModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<INotificationStore, DatabaseNotificationStore>();
        IocManager.Register<INotificationPublisher, DefaultNotificationPublisher>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NotificationModule).Assembly);
        
        var publisher = IocManager.Resolve<INotificationPublisher>();
        publisher.Initialize();
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpKernelModule))]
public class NotificationModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register all services
        IocManager.Register<INotificationStore, DatabaseNotificationStore>();
        IocManager.Register<INotificationPublisher, DefaultNotificationPublisher>();
        IocManager.RegisterAssemblyByConvention(typeof(NotificationModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Initialize publisher with resolved services
        var publisher = IocManager.Resolve<INotificationPublisher>();
        publisher.Initialize();
    }
}
```

### Example 3: Conditional Service Registration

**Before:**
```csharp
public class CacheModule : AbpModule
{
    public override void PreInitialize()
    {
        var cacheConfig = Configuration.Modules.Cache();
        
        if (cacheConfig.UseRedis)
        {
            IocManager.Register<ICacheManager, RedisCacheManager>();
        }
        else
        {
            IocManager.Register<ICacheManager, MemoryCacheManager>();
        }
    }
}
```

**After:**
```csharp
public class CacheModule : AbpModule
{
    public override void ConfigureServices()
    {
        var cacheConfig = Configuration.Modules.Cache();
        
        // Same conditional logic, just in ConfigureServices
        if (cacheConfig.UseRedis)
        {
            IocManager.Register<ICacheManager, RedisCacheManager>();
        }
        else
        {
            IocManager.Register<ICacheManager, MemoryCacheManager>();
        }
    }
    
    public override void Initialize()
    {
        // Test cache connection
        var cacheManager = IocManager.Resolve<ICacheManager>();
        Logger.Info($"Cache manager initialized: {cacheManager.GetType().Name}");
    }
}
```

## Web Module Migration

### Example 4: ASP.NET Core Module

**Before:**
```csharp
[DependsOn(typeof(AbpAspNetCoreModule))]
public class MyWebModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Modules.AbpWebCommon().SendAllExceptionsToClients = true;
        
        IocManager.Register<IMyWebConfiguration, MyWebConfiguration>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MyWebModule).Assembly);
        
        var webConfig = IocManager.Resolve<IMyWebConfiguration>();
        webConfig.ConfigureRoutes();
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpAspNetCoreModule))]
public class MyWebModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configuration
        Configuration.Modules.AbpWebCommon().SendAllExceptionsToClients = true;
        
        // Service registration
        IocManager.Register<IMyWebConfiguration, MyWebConfiguration>();
        IocManager.RegisterAssemblyByConvention(typeof(MyWebModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Use resolved services
        var webConfig = IocManager.Resolve<IMyWebConfiguration>();
        webConfig.ConfigureRoutes();
    }
}
```

### Example 5: API Controllers

**Before:**
```csharp
public class ApiModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
            .ForAll<IApplicationService>(typeof(ApiModule).Assembly, "app")
            .Build();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ApiModule).Assembly);
    }
}
```

**After:**
```csharp
public class ApiModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register services first
        IocManager.RegisterAssemblyByConvention(typeof(ApiModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Configure dynamic API controllers (requires resolved services)
        Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
            .ForAll<IApplicationService>(typeof(ApiModule).Assembly, "app")
            .Build();
    }
}
```

### Example 6: Authentication Configuration

**Before:**
```csharp
public class AuthModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IAuthenticationService, JwtAuthenticationService>();
        
        Configuration.Modules.Auth().JwtSecret = "my-secret-key";
        Configuration.Modules.Auth().TokenExpiration = TimeSpan.FromHours(24);
    }
    
    public override void Initialize()
    {
        var authService = IocManager.Resolve<IAuthenticationService>();
        authService.Initialize();
    }
}
```

**After:**
```csharp
public class AuthModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register authentication service
        IocManager.Register<IAuthenticationService, JwtAuthenticationService>();
        
        // Configure settings
        Configuration.Modules.Auth().JwtSecret = "my-secret-key";
        Configuration.Modules.Auth().TokenExpiration = TimeSpan.FromHours(24);
    }
    
    public override void Initialize()
    {
        // Initialize authentication service
        var authService = IocManager.Resolve<IAuthenticationService>();
        authService.Initialize();
    }
}
```

## Data Access Module Migration

### Example 7: Entity Framework Core Module

**Before:**
```csharp
[DependsOn(typeof(AbpEntityFrameworkCoreModule))]
public class MyDataModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Modules.AbpEfCore().AddDbContext<MyDbContext>(options =>
        {
            options.DbContextOptions.UseSqlServer(connectionString);
        });
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MyDataModule).Assembly);
        
        using (var dbContext = IocManager.Resolve<MyDbContext>())
        {
            dbContext.Database.Migrate();
        }
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpEntityFrameworkCoreModule))]
public class MyDataModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configure DbContext
        Configuration.Modules.AbpEfCore().AddDbContext<MyDbContext>(options =>
        {
            options.DbContextOptions.UseSqlServer(connectionString);
        });
        
        // Register repositories
        IocManager.RegisterAssemblyByConvention(typeof(MyDataModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Database initialization (uses resolved DbContext)
        using (var dbContext = IocManager.Resolve<MyDbContext>())
        {
            dbContext.Database.Migrate();
        }
    }
}
```

### Example 8: Custom Repository Registration

**Before:**
```csharp
public class RepositoryModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IUserRepository, UserRepository>(DependencyLifeStyle.Transient);
        IocManager.Register<IOrderRepository, OrderRepository>(DependencyLifeStyle.Transient);
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(RepositoryModule).Assembly);
    }
}
```

**After:**
```csharp
public class RepositoryModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register custom repositories
        IocManager.Register<IUserRepository, UserRepository>(DependencyLifeStyle.Transient);
        IocManager.Register<IOrderRepository, OrderRepository>(DependencyLifeStyle.Transient);
        
        // Register by convention
        IocManager.RegisterAssemblyByConvention(typeof(RepositoryModule).Assembly);
    }
}
```

### Example 9: Database Seeding

**Before:**
```csharp
public class SeedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SeedModule).Assembly);
        
        var seeder = IocManager.Resolve<IDatabaseSeeder>();
        seeder.Seed();
    }
}
```

**After:**
```csharp
public class SeedModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register seeder
        IocManager.RegisterAssemblyByConvention(typeof(SeedModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Run seeding (uses resolved services)
        var seeder = IocManager.Resolve<IDatabaseSeeder>();
        seeder.Seed();
    }
}
```

## Integration Module Migration

### Example 10: AutoMapper Integration

**Before:**
```csharp
[DependsOn(typeof(AbpAutoMapperModule))]
public class MyMappingModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Modules.AbpAutoMapper().Configurators.Add(config =>
        {
            config.CreateMap<User, UserDto>();
            config.CreateMap<Order, OrderDto>();
        });
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MyMappingModule).Assembly);
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpAutoMapperModule))]
public class MyMappingModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configure AutoMapper
        Configuration.Modules.AbpAutoMapper().Configurators.Add(config =>
        {
            config.CreateMap<User, UserDto>();
            config.CreateMap<Order, OrderDto>();
        });
        
        // Register services
        IocManager.RegisterAssemblyByConvention(typeof(MyMappingModule).Assembly);
    }
}
```

### Example 11: FluentValidation Integration

**Before:**
```csharp
[DependsOn(typeof(AbpFluentValidationModule))]
public class ValidationModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IValidator<CreateUserInput>, CreateUserInputValidator>();
        IocManager.Register<IValidator<UpdateUserInput>, UpdateUserInputValidator>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ValidationModule).Assembly);
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpFluentValidationModule))]
public class ValidationModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register validators
        IocManager.Register<IValidator<CreateUserInput>, CreateUserInputValidator>();
        IocManager.Register<IValidator<UpdateUserInput>, UpdateUserInputValidator>();
        
        // Register by convention
        IocManager.RegisterAssemblyByConvention(typeof(ValidationModule).Assembly);
    }
}
```

### Example 12: Redis Cache Integration

**Before:**
```csharp
[DependsOn(typeof(AbpRedisCacheModule))]
public class RedisCacheConfigModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Caching.UseRedis(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.DatabaseId = 0;
        });
    }
    
    public override void Initialize()
    {
        var cacheManager = IocManager.Resolve<ICacheManager>();
        Logger.Info($"Redis cache initialized: {cacheManager.GetType().Name}");
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpRedisCacheModule))]
public class RedisCacheConfigModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configure Redis
        Configuration.Caching.UseRedis(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.DatabaseId = 0;
        });
    }
    
    public override void Initialize()
    {
        // Test Redis connection
        var cacheManager = IocManager.Resolve<ICacheManager>();
        Logger.Info($"Redis cache initialized: {cacheManager.GetType().Name}");
    }
}
```

## Advanced Scenarios

### Example 13: Interceptor Registration

**Before:**
```csharp
public class AuditModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.Register<AuditInterceptor>(DependencyLifeStyle.Transient);
        
        IocManager.IocContainer.Kernel.ComponentRegistered += (key, handler) =>
        {
            if (typeof(IApplicationService).IsAssignableFrom(handler.ComponentModel.Implementation))
            {
                handler.ComponentModel.Interceptors.Add(
                    new InterceptorReference(typeof(AuditInterceptor))
                );
            }
        };
    }
}
```

**After (Autofac):**
```csharp
public class AuditModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register interceptor
        IocManager.Register<AuditInterceptor>(DependencyLifeStyle.Transient);
        
        // Configure Autofac interception
        var builder = ((IocManager)IocManager).Builder;
        
        builder.RegisterCallback(rb =>
        {
            rb.Registered += (sender, args) =>
            {
                var limitType = args.ComponentRegistration.Activator.LimitType;
                if (typeof(IApplicationService).IsAssignableFrom(limitType))
                {
                    args.ComponentRegistration
                        .InterceptedBy<AuditInterceptor>()
                        .EnableInterfaceInterceptors();
                }
            };
        });
    }
}
```

### Example 14: Plugin Loading

**Before:**
```csharp
public class PluginModule : AbpModule
{
    public override void PreInitialize()
    {
        var pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        var pluginAssemblies = Directory.GetFiles(pluginFolder, "*.dll")
            .Select(Assembly.LoadFrom);
        
        foreach (var assembly in pluginAssemblies)
        {
            IocManager.RegisterAssemblyByConvention(assembly);
        }
    }
}
```

**After:**
```csharp
public class PluginModule : AbpModule
{
    public override void ConfigureServices()
    {
        var pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        var pluginAssemblies = Directory.GetFiles(pluginFolder, "*.dll")
            .Select(Assembly.LoadFrom);
        
        // Register all plugin assemblies
        foreach (var assembly in pluginAssemblies)
        {
            IocManager.RegisterAssemblyByConvention(assembly);
        }
    }
    
    public override void Initialize()
    {
        // Initialize plugins
        var plugins = IocManager.ResolveAll<IPlugin>();
        foreach (var plugin in plugins)
        {
            plugin.Initialize();
            Logger.Info($"Plugin initialized: {plugin.Name}");
        }
    }
}
```

### Example 15: Multi-Tenancy Configuration

**Before:**
```csharp
public class MultiTenancyModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.MultiTenancy.IsEnabled = true;
        
        IocManager.Register<ITenantResolver, DomainTenantResolver>();
        IocManager.Register<ITenantStore, DatabaseTenantStore>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MultiTenancyModule).Assembly);
        
        var tenantStore = IocManager.Resolve<ITenantStore>();
        tenantStore.Initialize();
    }
}
```

**After:**
```csharp
public class MultiTenancyModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Enable multi-tenancy
        Configuration.MultiTenancy.IsEnabled = true;
        
        // Register tenant services
        IocManager.Register<ITenantResolver, DomainTenantResolver>();
        IocManager.Register<ITenantStore, DatabaseTenantStore>();
        IocManager.RegisterAssemblyByConvention(typeof(MultiTenancyModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Initialize tenant store
        var tenantStore = IocManager.Resolve<ITenantStore>();
        tenantStore.Initialize();
    }
}
```

### Example 16: Localization Configuration

**Before:**
```csharp
public class LocalizationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Localization.Languages.Add(new LanguageInfo("en", "English"));
        Configuration.Localization.Languages.Add(new LanguageInfo("es", "Spanish"));
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(LocalizationModule).Assembly);
        
        var localizationManager = IocManager.Resolve<ILocalizationManager>();
        localizationManager.AddSource(
            new DictionaryBasedLocalizationSource(
                "MyApp",
                new JsonEmbeddedFileLocalizationDictionaryProvider(
                    typeof(LocalizationModule).Assembly,
                    "MyApp.Localization"
                )
            )
        );
    }
}
```

**After:**
```csharp
public class LocalizationModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configure languages (doesn't require services)
        Configuration.Localization.Languages.Add(new LanguageInfo("en", "English"));
        Configuration.Localization.Languages.Add(new LanguageInfo("es", "Spanish"));
        
        // Register services
        IocManager.RegisterAssemblyByConvention(typeof(LocalizationModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Add localization source (requires resolved services)
        var localizationManager = IocManager.Resolve<ILocalizationManager>();
        localizationManager.AddSource(
            new DictionaryBasedLocalizationSource(
                "MyApp",
                new JsonEmbeddedFileLocalizationDictionaryProvider(
                    typeof(LocalizationModule).Assembly,
                    "MyApp.Localization"
                )
            )
        );
    }
}
```

### Example 17: Background Job Configuration

**Before:**
```csharp
[DependsOn(typeof(AbpBackgroundJobsModule))]
public class JobModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.BackgroundJobs.IsJobExecutionEnabled = true;
        
        IocManager.Register<IBackgroundJobStore, DatabaseBackgroundJobStore>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(JobModule).Assembly);
        
        var jobManager = IocManager.Resolve<IBackgroundJobManager>();
        jobManager.EnqueueAsync<SendEmailJob, SendEmailArgs>(new SendEmailArgs
        {
            To = "admin@example.com",
            Subject = "System Started"
        });
    }
}
```

**After:**
```csharp
[DependsOn(typeof(AbpBackgroundJobsModule))]
public class JobModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Configure background jobs
        Configuration.BackgroundJobs.IsJobExecutionEnabled = true;
        
        // Register job store
        IocManager.Register<IBackgroundJobStore, DatabaseBackgroundJobStore>();
        IocManager.RegisterAssemblyByConvention(typeof(JobModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Enqueue startup job
        var jobManager = IocManager.Resolve<IBackgroundJobManager>();
        jobManager.EnqueueAsync<SendEmailJob, SendEmailArgs>(new SendEmailArgs
        {
            To = "admin@example.com",
            Subject = "System Started"
        });
    }
}
```

### Example 18: Event Bus Configuration

**Before:**
```csharp
public class EventModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IEventHandler<UserCreatedEvent>, SendWelcomeEmailHandler>();
        IocManager.Register<IEventHandler<OrderPlacedEvent>, ProcessOrderHandler>();
    }
    
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(EventModule).Assembly);
        
        var eventBus = IocManager.Resolve<IEventBus>();
        eventBus.Register<UserCreatedEvent, SendWelcomeEmailHandler>();
        eventBus.Register<OrderPlacedEvent, ProcessOrderHandler>();
    }
}
```

**After:**
```csharp
public class EventModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register event handlers
        IocManager.Register<IEventHandler<UserCreatedEvent>, SendWelcomeEmailHandler>();
        IocManager.Register<IEventHandler<OrderPlacedEvent>, ProcessOrderHandler>();
        IocManager.RegisterAssemblyByConvention(typeof(EventModule).Assembly);
    }
    
    public override void Initialize()
    {
        // Subscribe to events
        var eventBus = IocManager.Resolve<IEventBus>();
        eventBus.Register<UserCreatedEvent, SendWelcomeEmailHandler>();
        eventBus.Register<OrderPlacedEvent, ProcessOrderHandler>();
    }
}
```

## Summary

Key takeaways from these examples:

1. **Move all `IocManager.Register()` calls to `ConfigureServices()`**
2. **Move all `RegisterAssemblyByConvention()` calls to `ConfigureServices()`**
3. **Keep service resolution and usage in `Initialize()` or `PostInitialize()`**
4. **Simple configuration can stay in `ConfigureServices()`**
5. **Configuration that requires resolved services goes in `Initialize()`**
6. **Database operations always go in `Initialize()`**
7. **Event subscriptions go in `Initialize()`**
8. **Interceptor registration goes in `ConfigureServices()`**

Following these patterns will ensure a smooth migration to the two-phase lifecycle.
