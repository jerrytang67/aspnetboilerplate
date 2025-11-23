using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Abp.Collections.Extensions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;
using Abp.EntityFrameworkCore.Configuration;
using Abp.EntityFrameworkCore.Repositories;
using Abp.EntityFrameworkCore.Uow;
using Abp.Modules;
using Abp.Orm;
using Abp.Reflection;
using Abp.Reflection.Extensions;
using Autofac;

namespace Abp.EntityFrameworkCore;

/// <summary>
/// This module is used to implement "Data Access Layer" in EntityFramework.
/// </summary>
[DependsOn(typeof(AbpEntityFrameworkCommonModule))]
public class AbpEntityFrameworkCoreModule : AbpModule {
    private ITypeFinder _typeFinder;
    private ILogger<AbpEntityFrameworkCoreModule> _logger;

    public AbpEntityFrameworkCoreModule() {
        // TypeFinder will be resolved from IocManager when needed
    }

    public AbpEntityFrameworkCoreModule(ITypeFinder typeFinder) {
        _typeFinder = typeFinder;
    }

    public override void ConfigureServices() {
        // Register EF Core configuration
        IocManager.Register<IAbpEfCoreConfiguration, AbpEfCoreConfiguration>();

        // Register assembly by convention
        IocManager.RegisterAssemblyByConvention(typeof(AbpEntityFrameworkCoreModule).GetAssembly());

        // Register DbContext provider
        var iocMgr = (IocManager)IocManager;
        iocMgr.Builder.RegisterGeneric(typeof(UnitOfWorkDbContextProvider<>))
            .As(typeof(IDbContextProvider<>))
            .InstancePerDependency();

        _logger = IocManager.Resolve<ILoggerFactory>().CreateLogger<AbpEntityFrameworkCoreModule>();
    }

    public override void Initialize() {
        // Register repositories and DbContext configurations
        // This must be done in Initialize because it requires resolved services (ITypeFinder, IDbContextEntityFinder)
        RegisterGenericRepositoriesAndMatchDbContexes();
    }

    private void RegisterGenericRepositoriesAndMatchDbContexes() {
        // Get TypeFinder - either from constructor injection or resolve from container
        var typeFinder = _typeFinder ?? IocManager.Resolve<ITypeFinder>();

        var dbContextTypes =
            typeFinder.Find(type => {
                var typeInfo = type.GetTypeInfo();
                return typeInfo.IsPublic &&
                       !typeInfo.IsAbstract &&
                       typeInfo.IsClass &&
                       typeof(AbpDbContext).IsAssignableFrom(type);
            });

        if (dbContextTypes.IsNullOrEmpty()) {
            _logger.LogWarning("No class found derived from AbpDbContext.");
            return;
        }

        using (IScopedIocResolver scope = IocManager.CreateScope()) {
            foreach (var dbContextType in dbContextTypes) {
                _logger.LogDebug("Registering DbContext: " + dbContextType.AssemblyQualifiedName);

                scope.Resolve<IEfGenericRepositoryRegistrar>().RegisterForDbContext(dbContextType, IocManager, EfCoreAutoRepositoryTypes.Default);

                var iocMgr = (IocManager)IocManager;
                var registrar = new EfCoreBasedSecondaryOrmRegistrar(dbContextType, scope.Resolve<IDbContextEntityFinder>());
                iocMgr.Builder.RegisterInstance(registrar)
                    .As<ISecondaryOrmRegistrar>()
                    .Named<ISecondaryOrmRegistrar>(Guid.NewGuid().ToString("N"))
                    .SingleInstance();
            }

            scope.Resolve<IDbContextTypeMatcher>().Populate(dbContextTypes);
        }
    }
}