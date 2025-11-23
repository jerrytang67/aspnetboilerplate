using Abp.Dependency;
using Abp.EntityFrameworkCore.Extensions;
using Abp.ZeroCore.SampleApp.Core;
using Abp.ZeroCore.SampleApp.EntityFramework;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.Zero;

public static class TestServiceCollectionRegistrar
{
    public static void Register(IIocManager iocManager)
    {
        RegisterServiceCollectionServices(iocManager);
        RegisterSqliteInMemoryDb(iocManager);
    }

    private static void RegisterServiceCollectionServices(IIocManager iocManager)
    {
        var services = new ServiceCollection();
        ServicesCollectionDependencyRegistrar.Register(services);
        
        // Use Autofac to populate services from IServiceCollection
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.Populate(services);
    }

    private static void RegisterSqliteInMemoryDb(IIocManager iocManager)
    {
        var builder = new DbContextOptionsBuilder<SampleAppDbContext>();

        var inMemorySqlite = new SqliteConnection("Data Source=:memory:");
        builder.UseSqlite(inMemorySqlite).AddAbpDbContextOptionsExtension();

        // Register using Autofac
        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(builder.Options)
            .As<DbContextOptions<SampleAppDbContext>>()
            .SingleInstance();

        inMemorySqlite.Open();
        new SampleAppDbContext(builder.Options).Database.EnsureCreated();
    }
}