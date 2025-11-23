using System;
using System.Threading;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Extensions;
using Abp.HtmlSanitizer;
using Abp.HtmlSanitizer.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using AbpAspNetCoreDemo.Core;
using AbpAspNetCoreDemo.Core.Application.Account;
using AbpAspNetCoreDemo.Db;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AbpAspNetCoreDemo;

[DependsOn(
    typeof(AbpAspNetCoreModule),
    typeof(AbpEntityFrameworkCoreModule),
    typeof(AbpHtmlSanitizerModule),
    typeof(AbpAspNetCoreDemoCoreModule)
)]
public class AbpAspNetCoreDemoModule : AbpModule {
    public static AsyncLocal<Action<IAbpStartupConfiguration>> ConfigurationAction =
        new AsyncLocal<Action<IAbpStartupConfiguration>>();

    public override void ConfigureServices() {
        // PHASE 1: Service Configuration (Before Container Build)

        // Register DbContext
        RegisterDbContextToSqliteInMemoryDb(IocManager);

        // Register assembly by convention
        IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetCoreDemoModule).GetAssembly());
    }


    public override void Initialize() {
        // TEST: Print all ApplicationService classes found in this assembly
        TestAssemblyScanning.PrintApplicationServices();

        // Configure caching
        Configuration.Caching.MemoryCacheOptions = new MemoryCacheOptions {
            SizeLimit = 2048
        };

        // Enable detailed error information
        Configuration.Modules.AbpWebCommon().SendAllExceptionsToClients = true;
        
        // Log all exceptions with details
        Configuration.Modules.AbpAspNetCore().DefaultWrapResultAttribute.LogError = true;

        Configuration.Modules.AbpAspNetCore()
            .CreateControllersForAppServices(
                typeof(AbpAspNetCoreDemoModule).GetAssembly(),
                "app", useConventionalHttpVerbs: true);

        var s = IocManager.Resolve<IAbpAspNetCoreConfiguration>() as AbpAspNetCoreConfiguration;

        var list2 = s.ControllerAssemblySettings;

        ConfigurationAction.Value?.Invoke(Configuration);

        // PHASE 2: Application Initialization (After Container Build)

        // Configure endpoint routing (requires resolved services)
        var aspNetCoreConfig = IocManager.Resolve<IAbpAspNetCoreConfiguration>();
        aspNetCoreConfig.EndpointConfiguration.Add(endpoints => {
            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        });
    }

    private static SqliteConnection _inMemorySqlite;
    private static bool _databaseCreated = false;

    private static void RegisterDbContextToSqliteInMemoryDb(IIocManager iocManager) {
        var builder = new DbContextOptionsBuilder<MyDbContext>();

        _inMemorySqlite = new SqliteConnection("Data Source=:memory:");
        builder.UseSqlite(_inMemorySqlite).AddAbpDbContextOptionsExtension();

        var iocMgr = (IocManager)iocManager;
        iocMgr.Builder.RegisterInstance(builder.Options)
            .As<DbContextOptions<MyDbContext>>()
            .SingleInstance();

        // Open connection to keep the in-memory database alive
        _inMemorySqlite.Open();

        // Register DbContext with lazy database creation
        iocMgr.Builder.Register(c => {
            var options = c.Resolve<DbContextOptions<MyDbContext>>();
            var ctx = new MyDbContext(options);

            // Create database schema on first access
            if (!_databaseCreated) {
                ctx.Database.EnsureCreated();
                _databaseCreated = true;
            }

            return ctx;
        }).As<MyDbContext>().InstancePerLifetimeScope();
    }
}