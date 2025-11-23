using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Abp.AspNetCore;

public class AbpAspNetCoreConventionalRegistrar : IConventionalDependencyRegistrar
{
    public void RegisterAssembly(IConventionalRegistrationContext context)
    {
        var iocManager = (IocManager)context.IocManager;
        ContainerBuilder builder;

        // If container is already built, create a new builder for updates
        // Otherwise, use the main builder
        if (iocManager.IocContainer != null)
        {
            builder = new ContainerBuilder();
        }
        else
        {
            builder = iocManager.Builder;
        }

        //Razor Pages
        builder.RegisterAssemblyTypes(context.Assembly)
            .Where(type => typeof(PageModel).IsAssignableFrom(type) &&
                          !type.GetTypeInfo().IsGenericTypeDefinition &&
                          !type.IsAbstract)
            // .AsSelf()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .InstancePerDependency();

        //ViewComponents
        builder.RegisterAssemblyTypes(context.Assembly)
            .Where(type => typeof(ViewComponent).IsAssignableFrom(type) &&
                          !type.GetTypeInfo().IsGenericTypeDefinition &&
                          !type.IsAbstract)
            // .AsSelf()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .InstancePerDependency();

        //PerWebRequest - In Autofac, this is InstancePerLifetimeScope
        builder.RegisterAssemblyTypes(context.Assembly)
            .Where(type => typeof(IPerWebRequestDependency).IsAssignableFrom(type) &&
                          !type.GetTypeInfo().IsGenericTypeDefinition &&
                          !type.IsAbstract)
            // .AsSelf()
            .AsImplementedInterfaces()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .InstancePerLifetimeScope();

        // Note: Autofac's Update method is obsolete/removed
        // If container is already built, these registrations won't take effect
        // until the container is rebuilt during initialization
    }
}
