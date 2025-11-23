using System;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Tests.Domain;
using Abp.EntityFrameworkCore.Tests.Ef;
using Abp.Modules;
using Abp.TestBase;
using Microsoft.EntityFrameworkCore;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.EntityFrameworkCore.Configuration;
using Abp.EntityFrameworkCore.Extensions;
using Abp.Reflection.Extensions;
using Microsoft.Data.Sqlite;
using Autofac;

namespace Abp.EntityFrameworkCore.Tests;

[DependsOn(typeof(AbpEntityFrameworkCoreModule), typeof(AbpTestBaseModule))]
public class EntityFrameworkCoreTestModule : AbpModule
{
    public override void ConfigureServices()
    {
        //BloggingDbContext
        RegisterBloggingDbContextToSqliteInMemoryDb(IocManager);

        //SupportDbContext
        RegisterSupportDbContextToSqliteInMemoryDb(IocManager);

        //Custom repository
        Configuration.ReplaceService<IRepository<Post, Guid>>(() =>
        {
            var iocMgr = (IocManager)IocManager;
            iocMgr.Builder.RegisterType<PostRepository>()
                .As<IRepository<Post, Guid>>()
                .As<IPostRepository>()
                .As<PostRepository>()
                .InstancePerDependency();
        });

        Configuration.IocManager.Register<IRepository<TicketListItem>, TicketListItemRepository>();

        IocManager.RegisterAssemblyByConvention(typeof(EntityFrameworkCoreTestModule).GetAssembly());

        Configuration.Modules.AbpEfCore().UseAbpQueryCompiler = true;

        Configuration.UnitOfWork.IsolationLevel = IsolationLevel.Unspecified;
    }


    public override void Initialize()
    {


        using (var context = IocManager.Resolve<BloggingDbContext>())
        {
            context.Database.ExecuteSqlRaw("CREATE VIEW BlogView AS SELECT Id, Name, Url FROM Blogs");
        }
    }


    private static void RegisterBloggingDbContextToSqliteInMemoryDb(IIocManager iocManager)
    {
        var builder = new DbContextOptionsBuilder<BloggingDbContext>();

        var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
        builder.UseSqlite(inMemorySqlite).AddAbpDbContextOptionsExtension();

        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(builder.Options)
            .As<DbContextOptions<BloggingDbContext>>()
            .SingleInstance();

        inMemorySqlite.Open();
        new BloggingDbContext(builder.Options).Database.EnsureCreated();
    }

    private static void RegisterSupportDbContextToSqliteInMemoryDb(IIocManager iocManager)
    {
        var builder = new DbContextOptionsBuilder<SupportDbContext>();

        var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
        builder.UseSqlite(inMemorySqlite).AddAbpDbContextOptionsExtension();

        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(builder.Options)
            .As<DbContextOptions<SupportDbContext>>()
            .SingleInstance();

        inMemorySqlite.Open();
        var ctx = new SupportDbContext(builder.Options);
        ctx.Database.EnsureCreated();

        using (var command = ctx.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = SupportDbContext.TicketViewSql;
            ctx.Database.OpenConnection();

            command.ExecuteNonQuery();
        }
    }
}