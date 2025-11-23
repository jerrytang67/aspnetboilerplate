using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Reflection.Extensions;
using Autofac;
using Abp.Logging;

namespace Abp.EntityFramework.Repositories {
    public class EfGenericRepositoryRegistrar(IDbContextEntityFinder dbContextEntityFinder, ILogger<EfGenericRepositoryRegistrar> logger)
        : IEfGenericRepositoryRegistrar, ITransientDependency {
        private readonly ILogger<EfGenericRepositoryRegistrar> _logger = logger;

        public void RegisterForDbContext(
            Type dbContextType,
            IIocManager iocManager,
            AutoRepositoryTypesAttribute defaultAutoRepositoryTypesAttribute) {
            var autoRepositoryAttr = dbContextType.GetTypeInfo().GetSingleAttributeOrNull<AutoRepositoryTypesAttribute>() ?? defaultAutoRepositoryTypesAttribute;

            RegisterForDbContext(
                dbContextType,
                iocManager,
                autoRepositoryAttr.RepositoryInterface,
                autoRepositoryAttr.RepositoryInterfaceWithPrimaryKey,
                autoRepositoryAttr.RepositoryImplementation,
                autoRepositoryAttr.RepositoryImplementationWithPrimaryKey
            );

            if (autoRepositoryAttr.WithDefaultRepositoryInterfaces) {
                RegisterForDbContext(
                    dbContextType,
                    iocManager,
                    defaultAutoRepositoryTypesAttribute.RepositoryInterface,
                    defaultAutoRepositoryTypesAttribute.RepositoryInterfaceWithPrimaryKey,
                    autoRepositoryAttr.RepositoryImplementation,
                    autoRepositoryAttr.RepositoryImplementationWithPrimaryKey
                );
            }
        }

        public void RegisterForEntity(
            Type dbContextType,
            Type entityType,
            IIocManager iocManager,
            AutoRepositoryTypesAttribute defaultAutoRepositoryTypesAttribute) {
            var autoRepositoryAttr =
                dbContextType.GetTypeInfo().GetSingleAttributeOrNull<AutoRepositoryTypesAttribute>() ??
                defaultAutoRepositoryTypesAttribute;

            RegisterForEntity(
                dbContextType,
                entityType,
                iocManager,
                defaultAutoRepositoryTypesAttribute.RepositoryInterface,
                defaultAutoRepositoryTypesAttribute.RepositoryInterfaceWithPrimaryKey,
                autoRepositoryAttr.RepositoryImplementation,
                autoRepositoryAttr.RepositoryImplementationWithPrimaryKey
            );
        }

        private static void RegisterForEntity(
            Type dbContextType,
            Type entityType,
            IIocManager iocManager,
            Type repositoryInterface,
            Type repositoryInterfaceWithPrimaryKey,
            Type repositoryImplementation,
            Type repositoryImplementationWithPrimaryKey) {
            var iocMgr = (IocManager)iocManager;
            var builder = iocMgr.IocContainer != null ? new ContainerBuilder() : iocMgr.Builder;

            var primaryKeyType = EntityHelper.GetPrimaryKeyType(entityType);
            if (primaryKeyType == typeof(int)) {
                var genericRepositoryType = repositoryInterface.MakeGenericType(entityType);
                if (!iocManager.IsRegistered(genericRepositoryType)) {
                    var implType = repositoryImplementation.GetGenericArguments().Length == 1
                        ? repositoryImplementation.MakeGenericType(entityType)
                        : repositoryImplementation.MakeGenericType(dbContextType, entityType);

                    builder.RegisterType(implType)
                        .As(genericRepositoryType)
                        .Named(Guid.NewGuid().ToString("N"), genericRepositoryType)
                        .InstancePerDependency();
                }
            }

            var genericRepositoryTypeWithPrimaryKey = repositoryInterfaceWithPrimaryKey.MakeGenericType(
                entityType,
                primaryKeyType
            );

            if (!iocManager.IsRegistered(genericRepositoryTypeWithPrimaryKey)) {
                var implType = repositoryImplementationWithPrimaryKey.GetGenericArguments().Length == 2
                    ? repositoryImplementationWithPrimaryKey.MakeGenericType(entityType, primaryKeyType)
                    : repositoryImplementationWithPrimaryKey.MakeGenericType(dbContextType,
                        entityType, primaryKeyType);

                builder.RegisterType(implType)
                    .As(genericRepositoryTypeWithPrimaryKey)
                    .Named(Guid.NewGuid().ToString("N"), genericRepositoryTypeWithPrimaryKey)
                    .InstancePerDependency();
            }
        }

        private void RegisterForDbContext(
            Type dbContextType,
            IIocManager iocManager,
            Type repositoryInterface,
            Type repositoryInterfaceWithPrimaryKey,
            Type repositoryImplementation,
            Type repositoryImplementationWithPrimaryKey) {
            var iocMgr = (IocManager)iocManager;
            var builder = iocMgr.IocContainer != null ? new ContainerBuilder() : iocMgr.Builder;

            foreach (var entityTypeInfo in dbContextEntityFinder.GetEntityTypeInfos(dbContextType)) {
                var primaryKeyType = EntityHelper.GetPrimaryKeyType(entityTypeInfo.EntityType);
                if (primaryKeyType == typeof(int)) {
                    var genericRepositoryType = repositoryInterface.MakeGenericType(entityTypeInfo.EntityType);
                    if (!iocManager.IsRegistered(genericRepositoryType)) {
                        var implType = repositoryImplementation.GetGenericArguments().Length == 1
                            ? repositoryImplementation.MakeGenericType(entityTypeInfo.EntityType)
                            : repositoryImplementation.MakeGenericType(entityTypeInfo.DeclaringType,
                                entityTypeInfo.EntityType);

                        builder.RegisterType(implType)
                            .As(genericRepositoryType)
                            .Named(Guid.NewGuid().ToString("N"), genericRepositoryType)
                            .InstancePerDependency();
                    }
                }

                var genericRepositoryTypeWithPrimaryKey = repositoryInterfaceWithPrimaryKey.MakeGenericType(entityTypeInfo.EntityType, primaryKeyType);
                if (!iocManager.IsRegistered(genericRepositoryTypeWithPrimaryKey)) {
                    var implType = repositoryImplementationWithPrimaryKey.GetGenericArguments().Length == 2
                        ? repositoryImplementationWithPrimaryKey.MakeGenericType(entityTypeInfo.EntityType, primaryKeyType)
                        : repositoryImplementationWithPrimaryKey.MakeGenericType(entityTypeInfo.DeclaringType, entityTypeInfo.EntityType, primaryKeyType);

                    builder.RegisterType(implType)
                        .As(genericRepositoryTypeWithPrimaryKey)
                        .Named(Guid.NewGuid().ToString("N"), genericRepositoryTypeWithPrimaryKey)
                        .InstancePerDependency();
                }
            }
        }
    }
}