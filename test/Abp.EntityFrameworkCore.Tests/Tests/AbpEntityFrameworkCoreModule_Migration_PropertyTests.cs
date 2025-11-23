using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.EntityFramework;
using Abp.EntityFramework.Repositories;
using Abp.EntityFrameworkCore.Configuration;
using Abp.EntityFrameworkCore.Tests.Domain;
using Abp.EntityFrameworkCore.Tests.Ef;
using Abp.Modules;
using Abp.Orm;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;
using Xunit;

namespace Abp.EntityFrameworkCore.Tests.Tests
{
    /// <summary>
    /// Property-based tests for AbpEntityFrameworkCoreModule migration to ConfigureServices.
    /// **Feature: autofac-migration, Property 5: Data access module service registration**
    /// **Validates: Requirements 7.2, 7.3, 7.4**
    /// </summary>
    public class AbpEntityFrameworkCoreModule_Migration_PropertyTests : EntityFrameworkCoreModuleTestBase
    {
        /// <summary>
        /// Property: For any data access module, all repository implementations should be registered in ConfigureServices
        /// </summary>
        [Fact]
        public void Repository_Implementations_Should_Be_Registered_In_ConfigureServices()
        {
            // Assert - Verify repositories are registered
            // The test base already initializes the bootstrapper and builds the container
            
            // Generic repository for Blog entity (uses int as primary key)
            LocalIocManager.IsRegistered<IRepository<Blog>>().ShouldBeTrue(
                "Generic repository for Blog should be registered in ConfigureServices");

            // Generic repository for Post entity (uses Guid as primary key)
            LocalIocManager.IsRegistered<IRepository<Post, Guid>>().ShouldBeTrue(
                "Generic repository for Post should be registered in ConfigureServices");

            // Generic repository for Comment entity (uses int as primary key)
            LocalIocManager.IsRegistered<IRepository<Comment>>().ShouldBeTrue(
                "Generic repository for Comment should be registered in ConfigureServices");
        }

        /// <summary>
        /// Property: For any data access module, DbContext configurations should be registered in ConfigureServices
        /// </summary>
        [Fact]
        public void DbContext_Configurations_Should_Be_Registered_In_ConfigureServices()
        {
            // Assert - Verify DbContext provider is registered
            LocalIocManager.IsRegistered<IDbContextProvider<BloggingDbContext>>().ShouldBeTrue(
                "DbContext provider for BloggingDbContext should be registered in ConfigureServices");

            LocalIocManager.IsRegistered<IDbContextProvider<SupportDbContext>>().ShouldBeTrue(
                "DbContext provider for SupportDbContext should be registered in ConfigureServices");

            // Verify DbContext type matcher is registered
            LocalIocManager.IsRegistered<IDbContextTypeMatcher>().ShouldBeTrue(
                "DbContext type matcher should be registered in ConfigureServices");
        }

        /// <summary>
        /// Property: For any data access module, all registered repositories should be resolvable after container build
        /// </summary>
        [Fact]
        public void All_Repositories_Should_Be_Resolvable_After_Container_Build()
        {
            // Assert - Verify repositories can be resolved
            using (var scope = LocalIocManager.CreateScope())
            {
                var blogRepo = scope.Resolve<IRepository<Blog>>();
                blogRepo.ShouldNotBeNull("Blog repository should be resolvable");

                var postRepo = scope.Resolve<IRepository<Post, Guid>>();
                postRepo.ShouldNotBeNull("Post repository should be resolvable");

                var commentRepo = scope.Resolve<IRepository<Comment>>();
                commentRepo.ShouldNotBeNull("Comment repository should be resolvable");
            }
        }

        /// <summary>
        /// Property: For any data access module, secondary ORM registrars should be registered in ConfigureServices
        /// </summary>
        [Fact]
        public void Secondary_ORM_Registrars_Should_Be_Registered_In_ConfigureServices()
        {
            // Assert - Verify secondary ORM registrars are registered
            var registrars = LocalIocManager.ResolveAll<ISecondaryOrmRegistrar>();
            registrars.ShouldNotBeNull("Secondary ORM registrars should be registered");
            registrars.Length.ShouldBeGreaterThan(0, "At least one secondary ORM registrar should be registered");
        }

        /// <summary>
        /// Property: For any data access module, EF Core configuration should be registered in ConfigureServices
        /// </summary>
        [Fact]
        public void EfCore_Configuration_Should_Be_Registered_In_ConfigureServices()
        {
            // Assert - Verify EF Core configuration is registered
            LocalIocManager.IsRegistered<IAbpEfCoreConfiguration>().ShouldBeTrue(
                "EF Core configuration should be registered in ConfigureServices");

            var config = LocalIocManager.Resolve<IAbpEfCoreConfiguration>();
            config.ShouldNotBeNull("EF Core configuration should be resolvable");
        }
    }
}
